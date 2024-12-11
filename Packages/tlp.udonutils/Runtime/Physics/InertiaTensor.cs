using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace TLP.UdonUtils.Runtime.Physics
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(InertiaTensor), ExecutionOrder)]
    public class InertiaTensor : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = CenterOfMass.ExecutionOrder + 1;

        [SerializeField]
        private Rigidbody body;

        [FormerlySerializedAs("useCustomInertiaOnEnable")]
        public bool InitOnStart;

        public Vector3 customInertiaTensor;
        public Vector3 customInertiaTensorRotation = Quaternion.identity.eulerAngles;


        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (InitOnStart) {
                UpdateTensor(customInertiaTensor, Quaternion.Euler(customInertiaTensorRotation));
            }

            LogTensor();
            return true;
        }


        public bool SetTensor(Vector3 tensor, Quaternion rotation) {
            if (!HasStartedOk) {
                Error("Not initialized");
                return false;
            }

            UpdateTensor(tensor, rotation);
            return true;
        }

        #region Internal
        private void UpdateTensor(Vector3 tensor, Quaternion rotation) {
            body.inertiaTensor = tensor;
            body.inertiaTensorRotation = rotation;
        }

        private void LogTensor() {
            DebugLog($"[{GetType()}.LogTensor] {body.gameObject.name}.inertiaTensor = {body.inertiaTensor}");
            DebugLog(
                    $"[{GetType()}.LogTensor] {body.gameObject.name}.inertiaTensorRotation = {body.inertiaTensorRotation.eulerAngles}"
            );
        }
        #endregion
    }
}