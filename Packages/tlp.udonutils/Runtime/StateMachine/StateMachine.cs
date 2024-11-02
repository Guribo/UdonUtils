using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Factories;
using TLP.UdonUtils.Runtime.Sources.Time;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.StateMachine
{
    /// <summary>
    /// Controls which <see cref="StateMachineState"/> and thus corresponding <see cref="StateMachineBehaviour"/>s are active.
    /// Which state is currently active is automatically synchronized across the network if set to manual sync.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(StateMachine), ExecutionOrder)]
    public class StateMachine : TlpBaseBehaviour
    {
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpFactory.ExecutionOrder + 100;
        #endregion

        #region Settings
        /// <summary>
        /// MUST NOT BE MODIFIED AT RUNTIME!!! (except when indices and AIState are reset to null/-1 respectively afterwards)
        /// </summary>
        [Tooltip("The first state is the default state the machine starts with")]
        [FormerlySerializedAs("allStates")]
        public StateMachineState[] AllStates;

        [Tooltip("If set, the state machine will automatically synchronize transition timing across the network")]
        public TlpNetworkTime OptionalNetworkTime;

        [Tooltip(
                "How far away in the future the transition should occur, should be higher than the worst ping in the lobby so that every player sees the transition at the exact same time.")]
        public float EventSyncDelay = 0.5f;
        #endregion

        #region Synced State
        [UdonSynced]
        internal int SyncedStateIndex = -1;

        [UdonSynced]
        internal float SyncedTransitionTime;

        #region Local Copy
        internal int WorkingStateIndex = -1;
        internal float WorkingTransitionTime;
        #endregion
        #endregion

        #region State
        internal int LocalStateIndex = -1;
        internal int ScheduledTransitionTarget = -1;
        internal StateMachineState DelayedTransitionTarget;
        #endregion

        #region Lifecycle
        public virtual void OnEnable() {
#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif
            if (IsInitialized()) {
                StateMachineState.enabled = true;
            }
        }

        public virtual void OnDisable() {
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            if (AllStates == null) {
                return;
            }

            foreach (var state in AllStates) {
                if (Utilities.IsValid(state)) {
                    state.enabled = false;
                }
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Scheduled network time of the pending or last state transition.
        /// Can be used to determine exact timing between clients.
        /// </summary>
        public float TransitionNetworkTime => WorkingTransitionTime;

        public StateMachineState StateMachineState { get; internal set; }

        /// <returns>false if not yet initialized, true otherwise</returns>
        [PublicAPI]
        public bool IsInitialized() {
#if TLP_DEBUG
            DebugLog(nameof(IsInitialized));
#endif
            return Utilities.IsValid(StateMachineState);
        }

        /// <summary>
        /// Restarts the current state if the StateMachine is initialized
        /// </summary>
        /// <returns>true on success, false otherwise</returns>
        public bool RestartCurrentState() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RestartCurrentState));
#endif
            #endregion

            if (!IsInitialized()) {
                Error("Not initialized");
                return false;
            }

            StateMachineState.enabled = false;
            // ReSharper disable once Unity.InefficientPropertyAccess restart the state in case it uses delayed messages
            StateMachineState.enabled = true;
            return true;
        }

        /// <param name="newStateIndex"></param>
        /// <returns>false if index invalid or caller not owner</returns>
        [PublicAPI]
        public bool OwnerSetNewState(int newStateIndex) {
#if TLP_DEBUG
            DebugLog(nameof(OwnerSetNewState));
#endif
            if (newStateIndex < 0) {
                Error($"{nameof(newStateIndex)} must be non-negative");
                return false;
            }

            if (AllStates == null) {
                Error($"{nameof(AllStates)} is null");
                return false;
            }

            if (newStateIndex >= AllStates.Length) {
                Error(
                        $"{nameof(newStateIndex)} {newStateIndex} is out of bounds of {nameof(AllStates)}[{AllStates.Length}]");
                return false;
            }

            if (newStateIndex != WorkingStateIndex) {
                if (!MarkNetworkDirty()) {
                    return false;
                }

                if (Utilities.IsValid(OptionalNetworkTime)) {
                    WorkingStateIndex = newStateIndex;
                    ScheduledTransitionTarget = newStateIndex;
                    WorkingTransitionTime = OptionalNetworkTime.Time() + EventSyncDelay;
                    SendCustomEventDelayedSeconds(nameof(Delayed_ActiveSyncedState), EventSyncDelay);
                    return true;
                }
            } else if (newStateIndex == LocalStateIndex) {
                return true;
            }

            if (IsInitialized()) {
                StateMachineState.enabled = false;
            }

            LocalStateIndex = newStateIndex;
            WorkingStateIndex = newStateIndex;
            var newState = AllStates[newStateIndex];
            StateMachineState = newState;
            newState.enabled = true;
            return true;
        }


        [PublicAPI]
        public bool LocalOnlySetNewState(int newStateIndex) {
#if TLP_DEBUG
            DebugLog(nameof(LocalOnlySetNewState));
#endif
            if (newStateIndex == LocalStateIndex) {
                return true;
            }

            if (newStateIndex < 0) {
                Error($"{nameof(newStateIndex)} must be non-negative");
                return false;
            }

            if (AllStates == null) {
                Error($"{nameof(AllStates)} is null");
                return false;
            }

            if (newStateIndex >= AllStates.Length) {
                Error(
                        $"{nameof(newStateIndex)} {newStateIndex} is out of bounds of {nameof(AllStates)}[{AllStates.Length}]");

                return false;
            }

            if (IsInitialized()) {
                StateMachineState.enabled = false;
            }

            LocalStateIndex = newStateIndex;
            var newState = AllStates[newStateIndex];
            StateMachineState = newState;
            newState.enabled = true;

            return true;
        }

        /// <summary>
        /// Schedule a delayed transition to the target state
        /// </summary>
        /// <remarks>There can only be a single delayed transition at once,
        /// calling this multiple times may trigger an early transition using
        /// the remaining delay of the first call</remarks>
        /// <param name="targetState"></param>
        /// <param name="delaySeconds"></param>
        public bool TransitionToDelayed(StateMachineState targetState, float delaySeconds) {
            if (!Utilities.IsValid(targetState)) {
                Error($"{nameof(targetState)} invalid");
                return false;
            }

            if (!Networking.IsOwner(gameObject)) {
                Error($"{nameof(TransitionToDelayed)} caller must be owner");
                return false;
            }

            DelayedTransitionTarget = targetState;
            SendCustomEventDelayedSeconds(nameof(Delayed_TransitionToNext), delaySeconds);
            return true;
        }
        #endregion

        #region Network Events
        public override void OnPreSerialization() {
            base.OnPreSerialization();
            WriteNetworkState();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);
            ReadNetworkState();

            if (Utilities.IsValid(OptionalNetworkTime)) {
                if (TryScheduleDelayedTransition()) {
                    return;
                }
            }

            LocalOnlySetNewState(WorkingStateIndex);
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            base.OnOwnershipTransferred(player);
            if (!player.IsLocalSafe()) {
                return;
            }

            if (!RestartCurrentState()) {
                Error("Local player failed to restart current state after gaining ownership");
            }
        }
        #endregion

        #region Internal
        internal bool Initialize() {
#if TLP_DEBUG
            DebugLog(nameof(Initialize));
#endif
            if (IsInitialized()) {
                return true;
            }

            if (AllStates.LengthSafe() < 1) {
                Error($"{nameof(AllStates)} is empty");
                return false;
            }

            var firstState = AllStates[0];
            if (!Utilities.IsValid(firstState)) {
                Error("First state must be valid");
                return false;
            }

            if (!InitializeAllStates()) {
                return false;
            }

            if (OwnerSetNewState(0) || LocalOnlySetNewState(0)) {
                return true;
            }

            Error("Could not initialize state as non-owner");
            return false;
        }

        /// <summary>
        /// <see cref="AllStates"/> must not be null
        /// </summary>
        /// <returns>true on success, false if an element was invalid</returns>
        internal bool InitializeAllStates() {
#if TLP_DEBUG
            DebugLog(nameof(InitializeAllStates));
#endif
            for (int i = 0; i < AllStates.Length; ++i) {
                var state = AllStates[i];

                if (!Utilities.IsValid(state)) {
                    Error($"State at position {i} must be valid");
                    return false;
                }

                for (int j = 0; j < i; ++j) {
                    if (!ReferenceEquals(state, AllStates[j])) {
                        continue;
                    }

                    Error($"State at position {j} must only exist once in {nameof(AllStates)}");
                    return false;
                }
            }

            // only change the states if everything is valid
            for (int i = 0; i < AllStates.Length; ++i) {
                var state = AllStates[i];
                state.StateMachineIndex = i;
                state.StateMachine = this;
                state.enabled = false;
            }

            return true;
        }

        private bool TryScheduleDelayedTransition() {
            float delay = WorkingTransitionTime - OptionalNetworkTime.Time();
            if (delay <= 0f) {
                return false;
            }

            ScheduledTransitionTarget = WorkingStateIndex;
            SendCustomEventDelayedSeconds(nameof(Delayed_ActiveSyncedState), delay);
            return true;
        }

        private void ReadNetworkState() {
            WorkingStateIndex = SyncedStateIndex;
            WorkingTransitionTime = SyncedTransitionTime;
        }

        private void WriteNetworkState() {
            SyncedStateIndex = WorkingStateIndex;
            SyncedTransitionTime = WorkingTransitionTime;
        }

        #region Delayed
        /// <summary>
        /// DO NOT CALL DIRECTLY!
        /// Performs the scheduled transition to <see cref="DelayedTransitionTarget"/>
        /// </summary>
        public void Delayed_TransitionToNext() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Delayed_TransitionToNext));
#endif
            #endregion

            if (!Utilities.IsValid(DelayedTransitionTarget)) {
                Warn(
                        $"{nameof(Delayed_TransitionToNext)} had no target. {nameof(TransitionToDelayed)} was most likely called multiple times.");
                return;
            }

            if (!Networking.IsOwner(gameObject)) {
                Warn(
                        $"Caller of {nameof(Delayed_TransitionToNext)} has no longer ownership, aborting scheduled transition to {DelayedTransitionTarget.GetScriptPathInScene()}");
                return;
            }

            OwnerSetNewState(DelayedTransitionTarget.StateMachineIndex);
            DelayedTransitionTarget = null;
        }

        /// <summary>
        /// DO NOT CALL DIRECTLY!
        /// Performs the scheduled transition to <see cref="ScheduledTransitionTarget"/>
        /// </summary>
        public void Delayed_ActiveSyncedState() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Delayed_ActiveSyncedState));
#endif
            #endregion

            if (ScheduledTransitionTarget != WorkingStateIndex) {
                Warn("Another state is already pending, discarding delayed transition");
                return;
            }

            LocalOnlySetNewState(WorkingStateIndex);
        }
        #endregion
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            return Initialize();
        }
        #endregion
    }
}