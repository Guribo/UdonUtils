using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Rendering
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ReflectionProbeController), ExecutionOrder)]
    public class ReflectionProbeController : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.RecordingStart + 500;

        [FormerlySerializedAs("reflectionProbe")]
        public ReflectionProbe ReflectionProbe;

        [FormerlySerializedAs("updateInterval")]
        [Range(0, 60)]
        public float UpdateInterval = 10f;

        private int _renderId = int.MinValue;

        internal void OnEnable() {
            
            if (!Utilities.IsValid(ReflectionProbe)) {
                _Error($"{nameof(ReflectionProbe)} not set");
                return;
            }

            _UpdateReflections();
        }

        public void _UpdateReflections() {
            if (!(gameObject.activeInHierarchy
                  && enabled
                  && Utilities.IsValid(ReflectionProbe))) {
                return;
            }

            if (_renderId == int.MinValue || ReflectionProbe.IsFinishedRendering(_renderId)) {
                _renderId = ReflectionProbe.RenderProbe();
            }

            SendCustomEventDelayedSeconds(nameof(_UpdateReflections), UpdateInterval);
        }
    }
}