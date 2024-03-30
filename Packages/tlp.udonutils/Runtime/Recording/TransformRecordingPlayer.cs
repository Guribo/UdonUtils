using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Recording
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class TransformRecordingPlayer : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.Min;


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