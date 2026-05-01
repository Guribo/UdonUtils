using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonUtils.Runtime.Physics;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonUtils.Editor.Physics
{
    [CustomEditor(typeof(RigidbodyRecorder))]
    public class RigidbodyRecorderEditor : TlpBehaviourEditor
    {
        protected override string GetDescription()
        {
            return "Records the state of a rigidbody in a history over time.";
        }
    }
}
#endif

namespace TLP.UdonUtils.Runtime.Physics
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(RigidbodyRecorder), ExecutionOrder)]
    public class RigidbodyRecorder : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI] public new const int ExecutionOrder = PostLateUpdateVelocityProvider.ExecutionOrder + 1;
        #endregion

        #region Dependencies
        public RigidbodyVelocityProvider VelocityProvider;
        public RigidBodyPhysicsState PhysicsState;
        public RigidbodyHistory History;
        #endregion

        protected override bool SetupAndValidate()
        {
            if (!base.SetupAndValidate()) return false;

            if (!IsSet(PhysicsState, nameof(PhysicsState))) {
                return false;
            }
            
            if (!IsSet(History, nameof(History))) {
                return false;
            }

            if (!IsSet(VelocityProvider, nameof(VelocityProvider))) {
                return false;
            }

            if (!History.Initialize(1024)) {
                Error("Failed to initialize history");
                return false;
            }

            return true;
        }

        #region Lifecycle
        private void FixedUpdate()
        {
            if (!HasStartedOk) return;
            PhysicsState.Time = VelocityProvider.GetLatestSnapShot(
                    out PhysicsState.Position,
                    out PhysicsState.LinearVelocity,
                    out PhysicsState.LinearAcceleration,
                    out PhysicsState.Rotation,
                    out PhysicsState.AngularVelocityRadians,
                    out PhysicsState.AngularAcceleration,
                    out PhysicsState.RelativeTo,
                    out PhysicsState.CircleAngularVelocityDegrees);

            if (!History.AddFromSnapshot(PhysicsState)) {
                Error("Failed to add velocity from provider to history");
            }
        }
        #endregion
    }
}