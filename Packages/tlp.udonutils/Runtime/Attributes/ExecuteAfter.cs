#define DEBUG_EXECUTION_ORDER

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class ExecuteAfterAttribute : DefaultExecutionOrder
{
    public class Dependency
    {
        public int Order { get; set; }
        public HashSet<Type> ExecuteAfter { get; } = new HashSet<Type>();
    }

    internal static readonly Dictionary<Type, Dependency> s_HandledTypes = new Dictionary<Type, Dependency>();

    /// <summary>
    ///     Changes the execution order of the class it is attached to to be greater than all of the execution order values of
    ///     the Types provided.
    /// </summary>
    /// <param name="ownType">MonoBehaviour type this Attribute is attached to</param>
    /// <param name="types">Types of MonoBehaviours that shall execute before ownType</param>
    /// <param name="includingSubClasses">
    ///     Whether all SubClasses of each provided type shall also be executed before ownType.
    ///     Defaults to null which is interpreted as true for each given type, thus all SubClasses of given types shall be
    ///     executed before ownType.
    /// </param>
    /// <param name="executeAfterBaseType">Default: true. When true ownType is guaranteed to execute after its BaseType</param>
    /// <exception cref="ArgumentException">
    ///     When types is null or any given type is null or when a given type does not inherit
    ///     from MonoBehaviour
    /// </exception>
    public ExecuteAfterAttribute(
            Type ownType,
            Type[] types,
            bool[] includingSubClasses = null,
            bool executeAfterBaseType = true
    ) : base(
            DetermineOrder(
                    ownType,
                    types,
                    includingSubClasses,
                    executeAfterBaseType
            )
    ) {
        // nothing
    }

    private static int DetermineOrder(
            Type ownType,
            Type[] types,
            bool[] includingSubClasses,
            bool executeAfterBaseType
    ) {
        try {
            CheckArguments(ownType, types, includingSubClasses);

            // return known execution order if own type is already processed
            if (s_HandledTypes.TryGetValue(ownType, out var dependency)) {
                return dependency.Order;
            }

            // add the own type to the processed types
            dependency = new Dependency();
            s_HandledTypes.Add(ownType, dependency);

            var typesToDependOn = FindAllDirectAndIndirectDependencies(
                    ownType,
                    types,
                    includingSubClasses,
                    executeAfterBaseType
            );

            int max = 0;
            foreach (var type in typesToDependOn) {
                max = DetermineMaxOrderFromType(ownType, type, dependency, max);
            }

            // as we want to execute AFTER the found, highest execution order value we increment by 1
            ++max;
#if DEBUG_EXECUTION_ORDER
            Debug.Log($"Setting custom execution order of {ownType.Name} to {max}");
#endif
            dependency.Order = max;
            return max;
        }
        catch (Exception e) {
            // if we don't catch the exception Unity will just close without warning/error
            Debug.LogException(e);

            // in that case we just return the default execution order value
            return 0;
        }
    }

    private static int DetermineMaxOrderFromType(Type ownType, Type type, Dependency dependency, int max) {
        if (ownType == type) {
            // own type is not relevant for execution order determination
            return max;
        }

        // add type as dependency for other attributes to look up
        if (!dependency.ExecuteAfter.Contains(type)) {
            dependency.ExecuteAfter.Add(type);
        }

        // process already handled types
        if (s_HandledTypes.TryGetValue(type, out var othersDependency)) {
            CheckForCyclicDependency(ownType, othersDependency, type);
            max = UpdateMax(othersDependency, max);

            return max;
        }

        // process not yet handled types
        var customAttributes = type.GetCustomAttributes<DefaultExecutionOrder>(true);
        foreach (var defaultExecutionOrder in customAttributes) {
            if (s_HandledTypes.TryGetValue(type, out othersDependency)) {
                CheckForCyclicDependency(ownType, othersDependency, type);
                max = UpdateMax(othersDependency, max);
            } else {
                max = UpdateMax(defaultExecutionOrder, max);
            }
        }

        return max;
    }

    private static int UpdateMax(DefaultExecutionOrder defaultExecutionOrder, int max) {
        if (defaultExecutionOrder.order > max) {
            max = defaultExecutionOrder.order;
        }

        return max;
    }

    private static int UpdateMax(Dependency othersDependency, int max) {
        if (othersDependency.Order > max) {
            max = othersDependency.Order;
        }

        return max;
    }

    private static void CheckForCyclicDependency(Type ownType, Dependency othersDependency, Type type) {
        if (othersDependency.ExecuteAfter.Contains(ownType)) {
            throw new InvalidConstraintException(
                    $"In {nameof(ExecuteAfterAttribute)}: Cyclic dependency between {ownType.Name} and {type.Name}"
            );
        }
    }

    private static HashSet<Type> FindAllDirectAndIndirectDependencies(
            Type ownType,
            Type[] types,
            bool[] includingSubClasses,
            bool executeAfterBaseType
    ) {
        var typesToDependOn = new HashSet<Type>();
        var pending = new HashSet<Type>();
        for (int i = 0; i < types.Length; i++) {
            AddSpecifiedDependencies(types, includingSubClasses, i, pending);
        }

        if (executeAfterBaseType) {
            AddBaseTypeAsDependency(ownType, pending);
        }

        while (pending.Count > 0) {
            DetermineAllDirectAndIndirectDependencies(pending, typesToDependOn);
        }

        return typesToDependOn;
    }

    private static void DetermineAllDirectAndIndirectDependencies(
            HashSet<Type> pending,
            HashSet<Type> typesToDependOn
    ) {
        var first = pending.First();
        if (s_HandledTypes.TryGetValue(first, out var existingEntry)) {
            foreach (var type in existingEntry.ExecuteAfter) {
                if (pending.Contains(type)) {
                    continue;
                }

                if (typesToDependOn.Contains(type)) {
                    continue;
                }

                pending.Add(type);
            }
        }

        if (!typesToDependOn.Contains(first)) {
            typesToDependOn.Add(first);
        }

        pending.Remove(first);
    }

    private static void AddSpecifiedDependencies(
            Type[] types,
            bool[] includingSubClasses,
            int i,
            HashSet<Type> pending
    ) {
        var type = types[i];
        if (pending.Contains(type)) {
            return;
        }

        pending.Add(type);

        bool wantsToIncludeSubTypes = includingSubClasses == null || includingSubClasses[i];
        if (!wantsToIncludeSubTypes) {
            return;
        }

        AddAllRelevantSubClassesOfType(type, pending);
    }

    private static void AddAllRelevantSubClassesOfType(Type type, HashSet<Type> pending) {
        var subTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(foundType => foundType.IsSubclassOf(type));
        foreach (var subType in subTypes) {
            if (pending.Contains(subType)) {
                // already added
                continue;
            }

            pending.Add(subType);
        }
    }

    private static void AddBaseTypeAsDependency(Type ownType, HashSet<Type> pending) {
        var type = ownType.BaseType;
        if (type == null) {
            return;
        }

        if (type != typeof(MonoBehaviour) && !type.IsSubclassOf(typeof(MonoBehaviour))) {
            return;
        }

        if (pending.Contains(type)) {
            return;
        }

        pending.Add(type);
    }

    private static void CheckArguments(Type ownType, Type[] types, bool[] includingSubClasses) {
        CheckOwnTypeValidity(ownType);
        CheckDependencyDimensionValidity(ownType, types, includingSubClasses);
        CheckDependencyContentValidity(ownType, types);
    }

    private static void CheckDependencyContentValidity(Type ownType, Type[] types) {
        foreach (var type in types) {
            if (type == null) {
                throw new ArgumentNullException(
                        $"Found on {ownType}: {nameof(types)} contains null"
                );
            }

            if (type == typeof(MonoBehaviour)) {
                continue;
            }

            if (!type.IsSubclassOf(typeof(MonoBehaviour))) {
                throw new ArgumentException(
                        $"Found on {ownType}: {nameof(type)} {type.Name} must inherit from {nameof(MonoBehaviour)}"
                );
            }

            if (type != ownType) {
                continue;
            }

            throw new ArgumentException(
                    $"Found on {nameof(ExecuteAfterAttribute)}: {ownType.Name} can not depend on itself, skipping"
            );
        }
    }

    private static void CheckDependencyDimensionValidity(Type ownType, Type[] types, bool[] includingSubClasses) {
        if (types == null) {
            throw new ArgumentNullException($"Found on {ownType}: {nameof(types)} is null");
        }

        if (includingSubClasses == null) {
            return;
        }

        if (types.Length == includingSubClasses.Length) {
            return;
        }

        throw new ArgumentException(
                $"Found on {ownType}: {nameof(includingSubClasses)} must be the same length as {nameof(types)}"
        );
    }

    private static void CheckOwnTypeValidity(Type ownType) {
        if (ownType == null || !ownType.IsSubclassOf(typeof(MonoBehaviour))) {
            throw new ArgumentException($"{nameof(ownType)} must inherit from {nameof(MonoBehaviour)}");
        }
    }
}
