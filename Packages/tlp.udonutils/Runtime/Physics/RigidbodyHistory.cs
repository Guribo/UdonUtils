using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Physics
{
    /// <summary>
    /// Stores a fixed-size history of rigidbody motion snapshots captured during <c>FixedUpdate</c>,
    /// including linear and angular state used for recent motion analysis.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RigidbodyHistory : SnapshotHistory
    {
        #region State
        public int LastWriteIndex { get; private set; } = -1;

        /// <summary>
        /// Gets the number of valid snapshots currently stored in the history buffer.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Gets the maximum number of snapshots that can be stored before older entries are overwritten.
        /// </summary>
        public int Capacity { get; private set; }

        public bool Initialized => Capacity > 0;

        [HideInInspector] public double[] Time;
        [HideInInspector] public Vector3[] Position;
        [HideInInspector] public Vector3[] Velocity;
        [HideInInspector] public Vector3[] Acceleration;
        [HideInInspector] public Quaternion[] Rotation;
        [HideInInspector] public Vector3[] AngularVelocity;
        [HideInInspector] public Vector3[] AngularAcceleration;
        [HideInInspector] public Transform[] RelativeTo;
        [HideInInspector] public float[] CircleAngularVelocityDegrees;
        #endregion

        #region Overrides
        public bool Initialize(int size)
        {
            if (size < 1) return false;
            Capacity = size;
            Time = new double[Capacity];
            Position = new Vector3[Capacity];
            Velocity = new Vector3[Capacity];
            Acceleration = new Vector3[Capacity];
            Rotation = new Quaternion[Capacity];
            AngularVelocity = new Vector3[Capacity];
            AngularAcceleration = new Vector3[Capacity];
            RelativeTo = new Transform[Capacity];
            CircleAngularVelocityDegrees = new float[Capacity];

            return true;
        }
        #endregion

        #region Public API
        [PublicAPI]
        public override bool AddFromSnapshot(Snapshot snapshot)
        {
            if (Capacity <= 0) {
                return false;
            }

            var physicsState = (RigidBodyPhysicsState) snapshot;
            if (!Utilities.IsValid(physicsState)) {
                return false;
            }

            if (Size > 0 && snapshot.Time < Time[LastWriteIndex]) return false;

            LastWriteIndex = LastWriteIndex.AddLooping(Capacity);
            Size = Mathf.Min(Size + 1, Capacity);

            Time[LastWriteIndex] = physicsState.Time;
            Position[LastWriteIndex] = physicsState.Position;
            Velocity[LastWriteIndex] = physicsState.LinearVelocity;
            Acceleration[LastWriteIndex] = physicsState.LinearAcceleration;
            Rotation[LastWriteIndex] = physicsState.Rotation;
            AngularVelocity[LastWriteIndex] = physicsState.AngularVelocityRadians;
            CircleAngularVelocityDegrees[LastWriteIndex] = physicsState.CircleAngularVelocityDegrees;
            AngularAcceleration[LastWriteIndex] = physicsState.AngularAcceleration;
            RelativeTo[LastWriteIndex] = physicsState.RelativeTo;

            return true;
        }

        /// <summary>
        /// Determines whether the specified timestamp can be resolved from the current backlog
        /// for interpolation, or as an exact single-sample match.
        /// </summary>
        /// <param name="timestamp">The timestamp in seconds to test.</param>
        /// <returns><c>true</c> if the timestamp lies within the stored range.</returns>
        public bool Interpolatable(double timestamp)
        {
            return Size > 1 &&
                   Time[LastWriteIndex] >= timestamp &&
                   Time[GetOldestIndex()] <= timestamp;
        }

        private int GetOldestIndex()
        {
            return LastWriteIndex.AddLooping(Capacity, Capacity - Size + 1);
        }

        public bool Interpolate(double time, RigidBodyPhysicsState resultPhysicsState)
        {
            if (!Interpolatable(time)) return false;

            int left = GetOldestIndex();
            int right = Capacity - 1;
            if (LastWriteIndex < left) {
                // oldest sample is in second half of the buffer
                if (time < Time[right]) {
                    // value is between oldest index and end of buffer
                    left = BinarySearch(left, right, time);
                    right = left + 1;
                    InterpolateInternal(time, left, right, resultPhysicsState);
                    return true;
                }

                if (time > Time[0]) {
                    left = 0;
                    right = LastWriteIndex;

                    // value is between start of buffer and newest index
                    left = BinarySearch(left, right, time);
                    right = left + 1;
                    InterpolateInternal(time, left, right, resultPhysicsState);
                    return true;
                }

                left = right;
                right = 0;
                // value is between wrapping indices
                InterpolateInternal(time, left, right, resultPhysicsState);
                return true;
            }

            right = LastWriteIndex;
            // value is just between oldest and newest index
            left = BinarySearch(left, right, time);
            right = left + 1;
            InterpolateInternal(time, left, right, resultPhysicsState);
            return true;
        }

        private void InterpolateInternal(double time, int left, int right, RigidBodyPhysicsState resultPhysicsState)
        {
            float t = Mathf.InverseLerp(
                    0f,
                    (float)(Time[right] - Time[left]),
                    (float)(time - Time[left])
            );
            
            // TODO depending on the relative value,
            //  either A or B needs to be converted to be in the same space as the other
            resultPhysicsState.RelativeTo = t < 0.5f
                    ? RelativeTo[left]
                    : RelativeTo[right];

            resultPhysicsState.Time = Time[left] + (Time[right] - Time[left]) * t;
            resultPhysicsState.Position = Vector3.Lerp(
                    Position[left],
                    Position[right],
                    t);
            resultPhysicsState. Rotation = Quaternion.Slerp(
                    Rotation[left],
                    Rotation[right],
                    t);
            resultPhysicsState.LinearVelocity = Vector3.Lerp(
                    Velocity[left],
                    Velocity[right],
                    t);
            resultPhysicsState.AngularVelocityRadians = Vector3.Lerp(
                    AngularVelocity[left],
                    AngularVelocity[right],
                    t);
            resultPhysicsState.LinearAcceleration = Vector3.Lerp(
                    Acceleration[left],
                    Acceleration[right],
                    t);
            resultPhysicsState. CircleAngularVelocityDegrees = Mathf.Lerp(
                    CircleAngularVelocityDegrees[left],
                    CircleAngularVelocityDegrees[right],
                    t);
            resultPhysicsState.TeleportingFlipFlop = false; // TODO implement?
            resultPhysicsState.AngularAcceleration = Vector3.Lerp(
                    AngularAcceleration[left],
                    AngularAcceleration[right],
                    t);
        }

        private int BinarySearch(int left, int right, double time)
        {
            int mid = (left + right) / 2;
            while (right - left > 1) {
                if (time < Time[mid]) {
                    right = mid;
                } else {
                    left = mid;
                }
                mid = (left + right) / 2;
            }

            return left;
        }
        #endregion

        private void OnDrawGizmos()
        {
            if (Size > 1)
                Debug.DrawLine(
                        Position[LastWriteIndex],
                        Position[LastWriteIndex.SubtractLooping(Capacity)],
                        Color.red,
                        3f);
        }
    }
}