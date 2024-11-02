using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Recording;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Physics
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(CenterOfMass), ExecutionOrder)]
    public class CenterOfMass : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = ToggleObject.ExecutionOrder + 1;

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

        public override void Start() {
            base.Start();
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