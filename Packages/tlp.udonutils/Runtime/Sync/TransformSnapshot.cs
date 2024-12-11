using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Adapters.Cyan;
using TLP.UdonUtils.Runtime.Events;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TransformSnapshot), ExecutionOrder)]
    public class TransformSnapshot : TimeSnapshot
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = CyanPoolAdapter.ExecutionOrder + 1;

        public Quaternion Rotation;
        public Vector3 Position;
    }
}