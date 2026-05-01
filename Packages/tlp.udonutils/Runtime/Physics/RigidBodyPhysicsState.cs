using TLP.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonUtils.Runtime.Physics
{
    /// <summary>
    /// Represents a snapshot of a rigidbody's physics state for synchronization and prediction.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RigidBodyPhysicsState : Snapshot
    {
        public const byte BitMaskIsAfterCollision = 0b00000001;
        public const byte BitMaskIsTeleportingFlipFlop = 0b00000010;

        #region PublicAPI
        /// <summary>
        /// Gets or sets the world position of the rigidbody.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Gets or sets the linear velocity in world space.
        /// </summary>
        public Vector3 LinearVelocity;

        /// <summary>
        /// Gets or sets the linear acceleration in world space.
        /// </summary>
        public Vector3 LinearAcceleration;

        /// <summary>
        /// Gets or sets the world rotation of the rigidbody.
        /// </summary>
        public Quaternion Rotation = Quaternion.identity;

        /// <summary>
        /// Gets or sets the angular velocity in radians per second.
        /// </summary>
        public Vector3 AngularVelocityRadians;

        /// <summary>
        /// Gets or sets a toggle used to indicate that a teleport event occurred.
        /// </summary>
        public bool TeleportingFlipFlop;

        /// <summary>
        /// Gets or sets the angular velocity used for circular motion calculations, in degrees per second.
        /// </summary>
        public float CircleAngularVelocityDegrees;

        /// <summary>
        /// Gets or sets the angular acceleration in radians per second.
        /// </summary>
        public Vector3 AngularAcceleration;

        /// <summary>
        /// Gets or sets the transform that this physics state is relative to.
        /// </summary>
        public Transform RelativeTo;

        /// <summary>
        /// Indicates whether the rigidbody is in a state following a collision event.
        /// </summary>
        public bool IsAfterCollision;

        /// <summary>
        /// Copies all physics state values from another instance.
        /// </summary>
        /// <param name="other">The source physics state to copy from.</param>
        public override bool CopyFrom(Snapshot other)
        {
            var rigidBodyPhysicsState = (RigidBodyPhysicsState)other;
            if (!Utilities.IsValid(rigidBodyPhysicsState)) {
                return false;
            }

            Time = rigidBodyPhysicsState.Time;
            Position = rigidBodyPhysicsState.Position;
            LinearVelocity = rigidBodyPhysicsState.LinearVelocity;
            LinearAcceleration = rigidBodyPhysicsState.LinearAcceleration;
            Rotation = rigidBodyPhysicsState.Rotation;
            AngularVelocityRadians = rigidBodyPhysicsState.AngularVelocityRadians;
            TeleportingFlipFlop = rigidBodyPhysicsState.TeleportingFlipFlop;
            CircleAngularVelocityDegrees = rigidBodyPhysicsState.CircleAngularVelocityDegrees;
            AngularAcceleration = rigidBodyPhysicsState.AngularAcceleration;
            RelativeTo = rigidBodyPhysicsState.RelativeTo;
            IsAfterCollision = rigidBodyPhysicsState.IsAfterCollision;
            return true;
        }

        /// <summary>
        /// Sends this physics snapshot to another behaviour by invoking a named network event and passing
        /// the current motion state as event arguments.
        /// </summary>
        /// <param name="target">The behaviour that receives the network event.</param>
        /// <param name="playerTarget">Specifies which clients should receive the network event.</param>
        /// <param name="eventName">The name of the network event to invoke on the target behaviour.</param>
        public void SendViaNetworkEvent(
                TlpBaseBehaviour target,
                NetworkEventTarget playerTarget,
                string eventName
        )
        {
            target.SendCustomNetworkEvent(
                    playerTarget,
                    eventName,
                    LinearAcceleration,
                    Position,
                    Rotation,
                    LinearVelocity,
                    AngularVelocityRadians,
                    CircleAngularVelocityDegrees,
                    Time,
                    EncodeSyncedFlags());
        }

        public byte EncodeSyncedFlags()
        {
            byte flags = 0;
            if (TeleportingFlipFlop) {
                flags |= BitMaskIsTeleportingFlipFlop;
            }

            if (IsAfterCollision) {
                flags |= BitMaskIsAfterCollision;
            }

            return flags;
        }

        public void DecodeSyncedFlags(byte flags)
        {
            TeleportingFlipFlop = (flags & BitMaskIsTeleportingFlipFlop) != 0;
            IsAfterCollision = (flags & BitMaskIsAfterCollision) != 0;
        }


        /// <summary>
        /// Predicts this physics state forward to a target network time and writes the result into the provided state.
        /// Uses circular motion when the stored circle angular velocity exceeds the threshold; otherwise uses linear acceleration.
        /// </summary>
        /// <param name="networkTime">The target network time to predict the state for.</param>
        /// <param name="circleThreshold">The minimum circular angular velocity, in degrees per second, required to use circular prediction.</param>
        /// <param name="predictedState">The state instance that receives the predicted position, rotation, and motion values.</param>
        public void PredictState(
                double networkTime,
                float circleThreshold,
                RigidBodyPhysicsState predictedState)
        {
            double elapsed = networkTime - Time;
            if (CircleAngularVelocityDegrees > circleThreshold) {
                predictedState.Position = ConstantCircularVelocity.PositionOnCircle(
                        Position,
                        LinearVelocity,
                        LinearAcceleration,
                        CircleAngularVelocityDegrees,
                        (float)elapsed,
                        out var predictedVelocity,
                        out var rotationDelta);
                predictedState.LinearVelocity = predictedVelocity;
            } else {
                predictedState.Position = ConstantLinearAcceleration.Position(
                        Position,
                        LinearVelocity,
                        LinearAcceleration,
                        (float)elapsed);
                predictedState.LinearVelocity = ConstantLinearAcceleration.Velocity(
                        LinearVelocity,
                        LinearAcceleration,
                        (float)elapsed);
            }

            Quaternion.Euler(AngularVelocityRadians).ToAngleAxis(
                    out float syncedTurnRateRadians,
                    out var syncedTurnAxis
            );

            // conversion to degrees is done AFTER the axis is created, otherwise huge errors are introduced from euler angles
            float predictedTurnDelta = (float)(syncedTurnRateRadians * elapsed * Mathf.Rad2Deg);
            var rawDeltaRotation = Quaternion.AngleAxis(predictedTurnDelta, syncedTurnAxis);

            // apply deltaRotation in world space
            predictedState.Rotation = rawDeltaRotation * Rotation.normalized;
            predictedState.Time = networkTime;
            predictedState.LinearAcceleration = LinearAcceleration;
            predictedState.AngularVelocityRadians = AngularVelocityRadians;
            predictedState.TeleportingFlipFlop = TeleportingFlipFlop;
            predictedState.CircleAngularVelocityDegrees = CircleAngularVelocityDegrees;
            predictedState.RelativeTo = RelativeTo;
            predictedState.AngularAcceleration = AngularAcceleration;
        }

        /// <summary>
        /// Interpolates between two physics states and stores the blended result in this instance.
        /// </summary>
        /// <param name="stateA">The start state used when <paramref name="t"/> is 0.</param>
        /// <param name="stateB">The end state used when <paramref name="t"/> is 1.</param>
        /// <param name="t">The interpolation factor between the two states, typically in the range from 0 to 1.</param>
        public void Lerp(
                RigidBodyPhysicsState stateA,
                RigidBodyPhysicsState stateB,
                float t)
        {
            // TODO depending on the relative value,
            //  either A or B needs to be converted to be in the same space as the other
            RelativeTo = t < 0.5f
                    ? stateA.RelativeTo
                    : stateB.RelativeTo;

            Time = stateA.Time + (stateB.Time - stateA.Time) * t;
            Position = Vector3.Lerp(
                    stateA.Position,
                    stateB.Position,
                    t);
            Rotation = Quaternion.Slerp(
                    stateA.Rotation,
                    stateB.Rotation,
                    t);
            LinearVelocity = Vector3.Lerp(
                    stateA.LinearVelocity,
                    stateB.LinearVelocity,
                    t);
            AngularVelocityRadians = Vector3.Lerp(
                    stateA.AngularVelocityRadians,
                    stateB.AngularVelocityRadians,
                    t);
            LinearAcceleration = Vector3.Lerp(
                    stateA.LinearAcceleration,
                    stateB.LinearAcceleration,
                    t);
            CircleAngularVelocityDegrees = Mathf.Lerp(
                    stateA.CircleAngularVelocityDegrees,
                    stateB.CircleAngularVelocityDegrees,
                    t);
            TeleportingFlipFlop = t < 0.5f
                    ? stateA.TeleportingFlipFlop
                    : stateB.TeleportingFlipFlop;
            AngularAcceleration = Vector3.Lerp(
                    stateA.AngularAcceleration,
                    stateB.AngularAcceleration,
                    t);
        }
        #endregion
    }
}