using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Physics;
using TLP.UdonUtils.Runtime.Sync;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Recording
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TransformRecordingPlayer), ExecutionOrder)]
    public class TransformRecordingPlayer : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = InertiaTensor.ExecutionOrder + 1;

        [FormerlySerializedAs("transformRecorder")]
        public TransformRecorder TransformRecorder;

        [FormerlySerializedAs("playOnEnable")]
        public bool PlayOnEnable;

        internal float PlaybackStart;

        [SerializeField]
        private Transform Target;

        public void OnEnable() {
            if (!PlayOnEnable) {
                enabled = false;
                return;
            }

            PlaybackStart = Time.realtimeSinceStartup;
        }

        public void Update() {
            if (!Utilities.IsValid(Target)) {
                return;
            }

            float time = Time.realtimeSinceStartup - PlaybackStart;
            Target.SetPositionAndRotation(
                    TransformRecorder.GetPosition(time),
                    TransformRecorder.GetRotation(time)
            );
        }
    }
}