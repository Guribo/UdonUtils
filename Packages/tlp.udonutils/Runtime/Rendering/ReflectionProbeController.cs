using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Rendering
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ReflectionProbeController : TlpBaseBehaviour
    {
        public ReflectionProbe reflectionProbe;

        [Range(0, 60)]
        public float updateInterval = 10f;

        public void OnEnable() {
            if (!Assert(Utilities.IsValid(reflectionProbe), "reflectionProbe invalid", this)) {
                return;
            }

            UpdateReflections();
        }

        public void UpdateReflections() {
            if (!(gameObject.activeInHierarchy
                  && enabled
                  && Assert(Utilities.IsValid(reflectionProbe), "reflectionProbe invalid", this))) {
                return;
            }

            reflectionProbe.RenderProbe();

            SendCustomEventDelayedSeconds(nameof(UpdateReflections), updateInterval);
        }
    }
}