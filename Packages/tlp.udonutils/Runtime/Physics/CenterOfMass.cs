using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Physics
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class CenterOfMass : UdonSharpBehaviour
    {
        [SerializeField]
        private Rigidbody body;

        public bool useCustomCenterOfGravityOnEnable;
        public Transform centerOfMass;

        public void OnEnable() {
            if (useCustomCenterOfGravityOnEnable) {
                if (!Utilities.IsValid(centerOfMass)) {
                    Debug.LogError("Invalid centerOfGravity", gameObject);
                    return;
                }

                Set(centerOfMass.position);
            }

            LogCenterOfMass();
            enabled = false;
        }

        public void Start() {
            OnEnable();
        }

        public void Set(Vector3 worldPosition) {
            if (!Utilities.IsValid(body)) {
                Debug.LogError("Invalid Rigidbody", gameObject);
                return;
            }

            body.centerOfMass = body.transform.InverseTransformPoint(worldPosition);
        }

        private void LogCenterOfMass() {
            if (!Utilities.IsValid(body)) {
                Debug.LogError("Invalid Rigidbody", gameObject);
                return;
            }

            Debug.Log($"[{GetType()}.LogTensor] {body.gameObject.name}.centerOfMass = {body.centerOfMass}");
            Debug.Log($"[{GetType()}.LogTensor] {body.gameObject.name}.worldCenterOfMass = {body.worldCenterOfMass}");
        }
    }
}