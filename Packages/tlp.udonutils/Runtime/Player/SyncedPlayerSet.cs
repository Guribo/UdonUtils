using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Player
{
    /// <summary>
    /// Version of the <see cref="PlayerSet"/> that uses no networking
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(SyncedPlayerSet), ExecutionOrder)]
    public class SyncedPlayerSet : PlayerSet
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerSet.ExecutionOrder + 1; // no changes
    }
}