using System;
using System.Collections.Generic;
using System.Text;
using TLP.UdonUtils.Runtime.Logger;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
#if TLP_DEBUG
using System.Linq;
using System.Reflection;
#endif

[AttributeUsage(AttributeTargets.Class)]
public class TlpDefaultExecutionOrder : DefaultExecutionOrder
{
#if UNITY_EDITOR
    [MenuItem("Tools/TLP/UdonUtils/TLP ExecutionOrder/Validate Type Consistency")]
    public static void ValidateTlpDefaultExecutionOrder() {
        var executingAssembly = Assembly.GetExecutingAssembly();
        TlpLogger.StaticInfo($"Validating {executingAssembly.FullName}", typeof(TlpDefaultExecutionOrder));

        ReportDuplicateTargetTypes();
        ValidateTargetTypeConsistency();
    }
#endif


#if TLP_DEBUG
    private static readonly Dictionary<int, Type> s_executionOrders = new Dictionary<int, Type>();
    private static readonly Dictionary<Type, int> s_knownTypes = new Dictionary<Type, int>();

    private static int s_lowestFailedOrder = int.MaxValue;
    private readonly Type _targetType;

    /// <summary>
    /// Finds all classes that have TlpDefaultExecutionOrder attribute and groups them by target type
    /// </summary>
    /// <returns>Dictionary where key is the target type and value is list of classes using that target type</returns>
    private static Dictionary<Type, List<Type>> GroupClassesByTargetType() {
        var result = new Dictionary<Type, List<Type>>();

        var allTypes = Assembly.GetExecutingAssembly().GetTypes();

        foreach (var type in allTypes) {
            var attribute = type.GetCustomAttribute<TlpDefaultExecutionOrder>();
            if (attribute?._targetType == null) {
                continue;
            }

            if (!result.ContainsKey(attribute._targetType)) {
                result[attribute._targetType] = new List<Type>();
            }

            result[attribute._targetType].Add(type);
        }

        return result;
    }

    /// <summary>
    /// Finds duplicate target types (target types used by more than one class)
    /// </summary>
    /// <returns>Dictionary of target types that are used by multiple classes</returns>
    private static Dictionary<Type, List<Type>> FindDuplicateTargetTypes() {
        var allGroups = GroupClassesByTargetType();
        return allGroups.Where(kvp => kvp.Value.Count > 1)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }


    private static void ValidateTargetTypeConsistency() {
        var allTypes = Assembly.GetExecutingAssembly().GetTypes();

        foreach (var type in allTypes) {
            var attribute = type.GetCustomAttribute<TlpDefaultExecutionOrder>();
            if (attribute?._targetType == null) {
                continue;
            }

            if (attribute._targetType != type) {
                TlpLogger.StaticError(
                        $"{nameof(TlpDefaultExecutionOrder)}: Type '{type}' has a different target type than the attribute constructor: {attribute._targetType}",
                        type);
            }
        }
    }


    private static void ReportDuplicateTargetTypes() {
        var duplicates = FindDuplicateTargetTypes();
        foreach (var kvp in duplicates) {
            var sb = new StringBuilder();
            sb.Append($"Target type {kvp.Key.Name} is used by multiple classes:");
            foreach (var classType in kvp.Value) {
                sb.Append($"  - {classType.Name}\n");
            }

            if (kvp.Value.Count > 0) {
                TlpLogger.StaticError(sb.ToString(), typeof(TlpDefaultExecutionOrder));
            }
        }
    }

#endif
    public TlpDefaultExecutionOrder(Type type, int newExecutionOrder, int lowerLimit = int.MinValue, int upperLimit = int.MaxValue) : base(
            newExecutionOrder) {
#if TLP_DEBUG
        _targetType = type;

        if (newExecutionOrder <= lowerLimit) {
            TlpLogger.StaticError(
                    $"ExecutionOrder {newExecutionOrder} of '{type}' is lower than the lower limit of {lowerLimit}",
                    typeof(TlpDefaultExecutionOrder));
            return;
        }

        if (newExecutionOrder >= upperLimit) {
            TlpLogger.StaticError(
                    $"ExecutionOrder {newExecutionOrder} of '{type}' is higher than the upper limit of {upperLimit}",
                    typeof(TlpDefaultExecutionOrder));
            return;
        }

        if (s_knownTypes.TryGetValue(type, out int existingExecutionOrder)) {
            if (existingExecutionOrder != newExecutionOrder) {
                TlpLogger.StaticError(
                        $"'{type}' already added before with different ExecutionOrder: {existingExecutionOrder} (existing) != {newExecutionOrder} (new)",
                        typeof(TlpDefaultExecutionOrder));
            }

            return;
        }

        s_knownTypes[type] = newExecutionOrder;

        if (s_executionOrders.TryAdd(newExecutionOrder, type)) {
            return;
        }

        TlpLogger.StaticError(
                $"ExecutionOrder {newExecutionOrder} of '{type}' is already in use by '{s_executionOrders[newExecutionOrder]}'",
                typeof(TlpDefaultExecutionOrder));

        if (newExecutionOrder < s_lowestFailedOrder) {
            s_lowestFailedOrder = newExecutionOrder;
        }
#endif
    }
}