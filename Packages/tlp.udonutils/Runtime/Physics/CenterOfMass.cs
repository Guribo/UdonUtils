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

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(Body)) {
                _Error($"{nameof(Body)} not set");
                return false;
            }

            if (!Utilities.IsValid(CenterOfMassPosition)) {
                _Error($"{nameof(CenterOfMassPosition)} not set");
                return false;
            }

            if (InitOnStart) {
                _UpdateCenterOfMass(CenterOfMassPosition.position);
            }

            _LogCenterOfMass();
            return true;
        }

        public bool _Set(Vector3 worldPosition) {
            if (!HasStartedOk) {
                _Error("Not initialized");
                return false;
            }

            _UpdateCenterOfMass(worldPosition);
            return true;
        }


        #region Internal
        private void _UpdateCenterOfMass(Vector3 worldPosition) {
            Body.centerOfMass = Body.transform.InverseTransformPoint(worldPosition);
        }

        private void _LogCenterOfMass() {
            _DebugLog($"[{GetType()}.LogTensor] {Body.gameObject.name}.centerOfMassPosition = {Body.centerOfMass}");
            _DebugLog($"[{GetType()}.LogTensor] {Body.gameObject.name}.worldCenterOfMass = {Body.worldCenterOfMass}");
        }
        #endregion
    }
}