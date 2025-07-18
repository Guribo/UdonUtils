using System;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class TlpDefaultExecutionOrder : DefaultExecutionOrder
{
#if TLP_DEBUG
    private static readonly Dictionary<int, Type> s_executionOrders = new Dictionary<int, Type>();
    private static readonly HashSet<Type> s_executionOrdersByType = new HashSet<Type>();

    private static int s_lowestFailedOrder = int.MaxValue;
#endif
    public TlpDefaultExecutionOrder(Type type, int order) : base(order) {
#if TLP_DEBUG
        if (!s_executionOrdersByType.Add(type)) {
            Debug.LogWarning($"Type {type} already exists");
            return;
        }

        if (s_executionOrders.TryAdd(order, type)) {
            return;
        }

        if (order < s_lowestFailedOrder) {
            s_lowestFailedOrder = order;
            Debug.LogError($"ExecutionOrder value {order} of {type} is already in use by {s_executionOrders[order]}");
            return;
        }

        Debug.LogWarning($"ExecutionOrder value {order} of {type} is already in use by {s_executionOrders[order]}");
#endif
    }
}