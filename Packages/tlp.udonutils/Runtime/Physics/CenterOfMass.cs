using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Physics
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(CenterOfMass), ExecutionOrder)]
    public class CenterOfMass : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = ToggleObject.ExecutionOrder + 1;
        #endregion

        [FormerlySerializedAs("body")]
        [SerializeField]
        private Rigidbody Body;

        [FormerlySerializedAs("useCustomCenterOfGravityOnEnable")]
        public bool InitOnStart;

        [FormerlySerializedAs("centerOfMassPosition")]
        [FormerlySerializedAs("centerOfMass")]
        public Transform CenterOfMassPosition;

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(Body)) {
                Error($"{nameof(Body)} not set");
                return false;
            }

            if (!Utilities.IsValid(CenterOfMassPosition)) {
                Error($"{nameof(CenterOfMassPosition)} not set");
                return false;
            }

            if (InitOnStart) {
                UpdateCenterOfMass(CenterOfMassPosition.position);
            }

            LogCenterOfMass();
            return true;
        }

        public bool Set(Vector3 worldPosition) {
            if (!HasStartedOk) {
                Error("Not initialized");
                return false;
            }

            UpdateCenterOfMass(worldPosition);
            return true;
        }


        #region Internal
        private void UpdateCenterOfMass(Vector3 worldPosition) {
            Body.centerOfMass = Body.transform.InverseTransformPoint(worldPosition);
        }

        private void LogCenterOfMass() {
            DebugLog($"[{GetType()}.LogTensor] {Body.gameObject.name}.centerOfMassPosition = {Body.centerOfMass}");
            DebugLog($"[{GetType()}.LogTensor] {Body.gameObject.name}.worldCenterOfMass = {Body.worldCenterOfMass}");
        }
        #endregion
    }
}