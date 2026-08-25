using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonUtils.Runtime.Physics;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonUtils.Editor
{
    [CustomEditor(typeof(InertiaTensor))]
    public class InertiaTensorEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Helper that sets a custom inertia tensor and rotation during on during ininitialization.";
        }
    }
}
#endif

namespace TLP.UdonUtils.Runtime.Physics
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(InertiaTensor), ExecutionOrder)]
    public class InertiaTensor : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;
        public new const int ExecutionOrder = CenterOfMass.ExecutionOrder + 1;
        #endregion

        #region Dependencies
        [Header("Dependencies")]
        [FormerlySerializedAs("body")]
        [PublicAPI]
        [SerializeField]
        private Rigidbody Body;
        #endregion

        #region Configuration
        [FormerlySerializedAs("useCustomInertiaOnEnable")]
        public bool InitOnStart;

        [FormerlySerializedAs("customInertiaTensor")]
        public Vector3 CustomInertiaTensor;

        [FormerlySerializedAs("customInertiaTensorRotation")]
        public Vector3 CustomInertiaTensorRotation = Quaternion.identity.eulerAngles;
        #endregion

        #region Lifecycle
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_IsSet(Body, nameof(Body))) {
                return false;
            }

            if (InitOnStart) {
                _SetTensor(CustomInertiaTensor, Quaternion.Euler(CustomInertiaTensorRotation));
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"Initial {nameof(Body.inertiaTensor)} = {Body.inertiaTensor}");
            _DebugLog($"Initial {nameof(Body.inertiaTensorRotation)} = {Body.inertiaTensorRotation.eulerAngles}");
#endif
            #endregion

            return true;
        }
        #endregion

        #region Public API
        public bool _SetTensor(Vector3 tensor, Quaternion rotation) {
            if (!IsReceivingStart && !HasStartedOk) {
                return false;
            }

            Body.inertiaTensor = tensor;
            Body.inertiaTensorRotation = rotation;
            return true;
        }
        #endregion
    }
}