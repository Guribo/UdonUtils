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
        public override int ExecutionOrderReadOnly => ExecutionOrder;

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
        internal double SyncedTransitionTime;

        #region Local Copy
        internal int WorkingStateIndex = -1;
        internal double WorkingTransitionTime;
        #endregion
        #endregion

        #region State
        internal int LocalStateIndex = -1;
        internal int ScheduledTransitionTarget = -1;
        internal StateMachineState DelayedTransitionTarget;
        #endregion

        #region Lifecycle
        protected internal virtual void OnEnable() {
#if TLP_DEBUG
            _DebugLog(nameof(OnEnable));
#endif
            if (_IsInitialized()) {
                StateMachineState.enabled = true;
            }
        }

        protected internal virtual void OnDisable() {
#if TLP_DEBUG
            _DebugLog(nameof(OnDisable));
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
        public double TransitionNetworkTime => WorkingTransitionTime;

        public StateMachineState StateMachineState { get; internal set; }

        /// <returns>false if not yet initialized, true otherwise</returns>
        [PublicAPI]
        public bool _IsInitialized() {
#if TLP_DEBUG
            _DebugLog(nameof(_IsInitialized));
#endif
            return Utilities.IsValid(StateMachineState);
        }

        /// <summary>
        /// Restarts the current state if the StateMachine is initialized
        /// </summary>
        /// <returns>true on success, false otherwise</returns>
        public bool _RestartCurrentState() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RestartCurrentState));
#endif
            #endregion

            if (!_IsInitialized()) {
                _Error("Not initialized");
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
        public bool _OwnerSetNewState(int newStateIndex) {
#if TLP_DEBUG
            _DebugLog(nameof(_OwnerSetNewState));
#endif
            if (newStateIndex < 0) {
                _Error($"{nameof(newStateIndex)} must be non-negative");
                return false;
            }

            if (AllStates == null) {
                _Error($"{nameof(AllStates)} is null");
                return false;
            }

            if (newStateIndex >= AllStates.Length) {
                _Error(
                        $"{nameof(newStateIndex)} {newStateIndex} is out of bounds of {nameof(AllStates)}[{AllStates.Length}]");
                return false;
            }

            if (newStateIndex != WorkingStateIndex) {
                if (!_MarkNetworkDirty()) {
                    return false;
                }

                if (Utilities.IsValid(OptionalNetworkTime)) {
                    WorkingStateIndex = newStateIndex;
                    ScheduledTransitionTarget = newStateIndex;
                    WorkingTransitionTime = OptionalNetworkTime._TimeAsDouble() + EventSyncDelay;
                    SendCustomEventDelayedSeconds(nameof(_Delayed_ActiveSyncedState), EventSyncDelay);
                    return true;
                }
            } else if (newStateIndex == LocalStateIndex) {
                return true;
            }

            if (_IsInitialized()) {
                StateMachineState.enabled = false;
            }

            var newState = AllStates[newStateIndex];
            if (!Utilities.IsValid(newState)) {
                _Error($"{nameof(_OwnerSetNewState)}: {nameof(AllStates)} is null at index {newStateIndex}");
                return false;
            }

            LocalStateIndex = newStateIndex;
            WorkingStateIndex = newStateIndex;
            StateMachineState = newState;
            newState.enabled = true;
            return true;
        }


        [PublicAPI]
        public bool _LocalOnlySetNewState(int newStateIndex) {
#if TLP_DEBUG
            _DebugLog(nameof(_LocalOnlySetNewState));
#endif
            if (newStateIndex == LocalStateIndex) {
                return true;
            }

            if (newStateIndex < 0) {
                _Error($"{nameof(newStateIndex)} must be non-negative");
                return false;
            }

            if (AllStates == null) {
                _Error($"{nameof(AllStates)} is null");
                return false;
            }

            if (newStateIndex >= AllStates.Length) {
                _Error(
                        $"{nameof(newStateIndex)} {newStateIndex} is out of bounds of {nameof(AllStates)}[{AllStates.Length}]");

                return false;
            }

            if (_IsInitialized()) {
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
        public bool _TransitionToDelayed(StateMachineState targetState, float delaySeconds) {
            if (!Utilities.IsValid(targetState)) {
                _Error($"{nameof(targetState)} invalid");
                return false;
            }

            if (!Networking.IsOwner(gameObject)) {
                _Error($"{nameof(_TransitionToDelayed)} caller must be owner");
                return false;
            }

            DelayedTransitionTarget = targetState;
            SendCustomEventDelayedSeconds(nameof(_Delayed_TransitionToNext), delaySeconds);
            return true;
        }
        #endregion

        #region Network Events
        public override void OnPreSerialization() {
            base.OnPreSerialization();
            _WriteNetworkState();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);
            _ReadNetworkState();

            if (Utilities.IsValid(OptionalNetworkTime)) {
                if (_TryScheduleDelayedTransition()) {
                    return;
                }
            }

            _LocalOnlySetNewState(WorkingStateIndex);
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            base.OnOwnershipTransferred(player);
            if (!player._IsLocalSafe()) {
                return;
            }

            if (!_RestartCurrentState()) {
                _Error("Local player failed to restart current state after gaining ownership");
            }
        }
        #endregion

        #region Internal
        internal bool _Initialize() {
#if TLP_DEBUG
            _DebugLog(nameof(_Initialize));
#endif
            if (_IsInitialized()) {
                return true;
            }

            if (AllStates._LengthSafe() < 1) {
                _Error($"{nameof(AllStates)} is empty");
                return false;
            }

            var firstState = AllStates[0];
            if (!Utilities.IsValid(firstState)) {
                _Error("First state must be valid");
                return false;
            }

            if (!_InitializeAllStates()) {
                return false;
            }

            if (_OwnerSetNewState(0) || _LocalOnlySetNewState(0)) {
                return true;
            }

            _Error("Could not initialize state as non-owner");
            return false;
        }

        /// <summary>
        /// <see cref="AllStates"/> must not be null
        /// </summary>
        /// <returns>true on success, false if an element was invalid</returns>
        internal bool _InitializeAllStates() {
#if TLP_DEBUG
            _DebugLog(nameof(_InitializeAllStates));
#endif
            for (int i = 0; i < AllStates.Length; ++i) {
                var state = AllStates[i];

                if (!Utilities.IsValid(state)) {
                    _Error($"State at position {i} must be valid");
                    return false;
                }

                for (int j = 0; j < i; ++j) {
                    if (!ReferenceEquals(state, AllStates[j])) {
                        continue;
                    }

                    _Error($"State at position {j} must only exist once in {nameof(AllStates)}");
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

        private bool _TryScheduleDelayedTransition() {
            double delay = WorkingTransitionTime - OptionalNetworkTime._Time();
            if (delay <= 0f) {
                return false;
            }

            ScheduledTransitionTarget = WorkingStateIndex;
            SendCustomEventDelayedSeconds(nameof(_Delayed_ActiveSyncedState), (float)delay);
            return true;
        }

        private void _ReadNetworkState() {
            WorkingStateIndex = SyncedStateIndex;
            WorkingTransitionTime = SyncedTransitionTime;
        }

        private void _WriteNetworkState() {
            SyncedStateIndex = WorkingStateIndex;
            SyncedTransitionTime = WorkingTransitionTime;
        }

        #region Delayed
        /// <summary>
        /// DO NOT CALL DIRECTLY!
        /// Performs the scheduled transition to <see cref="DelayedTransitionTarget"/>
        /// </summary>
        public void _Delayed_TransitionToNext() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Delayed_TransitionToNext));
#endif
            #endregion

            if (!Utilities.IsValid(DelayedTransitionTarget)) {
                _Warn(
                        $"{nameof(_Delayed_TransitionToNext)} had no target. {nameof(_TransitionToDelayed)} was most likely called multiple times.");
                return;
            }

            if (!Networking.IsOwner(gameObject)) {
                _Warn(
                        $"Caller of {nameof(_Delayed_TransitionToNext)} has no longer ownership, aborting scheduled transition to {DelayedTransitionTarget._GetScriptPathInScene()}");
                return;
            }

            _OwnerSetNewState(DelayedTransitionTarget.StateMachineIndex);
            DelayedTransitionTarget = null;
        }

        /// <summary>
        /// DO NOT CALL DIRECTLY!
        /// Performs the scheduled transition to <see cref="ScheduledTransitionTarget"/>
        /// </summary>
        public void _Delayed_ActiveSyncedState() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Delayed_ActiveSyncedState));
#endif
            #endregion

            if (ScheduledTransitionTarget != WorkingStateIndex) {
                _Warn("Another state is already pending, discarding delayed transition");
                return;
            }

            _LocalOnlySetNewState(WorkingStateIndex);
        }
        #endregion
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            return _Initialize();
        }
        #endregion
    }
}