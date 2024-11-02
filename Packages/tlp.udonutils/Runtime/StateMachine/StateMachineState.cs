using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.StateMachine
{
    /// <summary>
    /// Defines the state the state machine is in, including which behaviours may be used.
    /// 
    /// MUST NOT BE USED BY MULTIPLE STATE MACHINES AT THE SAME TIME!!!
    ///
    /// Is initialized by the state machine when the state machine is initialized
    /// 
    /// Can be extended to implement <see cref="OnBeginEnterState"/>,
    /// <see cref="OnStateEntered"/>, <see cref="OnBeginExitState"/>
    /// and <see cref="OnStateExited"/>.
    ///
    /// Should be extended to implement conditions for transitioning to other states.
    /// Alternatively transition logic can be implemented in dedicated <see cref="StateMachineBehaviour"/>s.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(StateMachineState), ExecutionOrder)]
    public class StateMachineState : TlpBaseBehaviour
    {
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = StateMachine.ExecutionOrder + 100;
        #endregion

        #region State
        [NonSerialized]
        [PublicAPI]
        public int StateMachineIndex = -1;

        [NonSerialized]
        [PublicAPI]
        public StateMachine StateMachine;
        #endregion

        #region Settings
        [PublicAPI]
        [Tooltip(
                "Behaviours to be dis-/enabled and de-/activation of this state. Will be disabled in reverse-order when this state is exited.")]
        public StateMachineBehaviour[] StateMachineBehaviours;
        #endregion

        #region Lifecycle
        public virtual void OnEnable() {
#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif
            if (StateMachineIndex == -1) {
                DisableBehaviours();
                enabled = false;
                return;
            }

            OnBeginEnterState();
            EnableBehaviours();
            OnStateEntered();
        }


        public virtual void OnDisable() {
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif

            if (StateMachineIndex == -1) {
                return;
            }

            OnBeginExitState();
            DisableBehaviours();
            OnStateExited();
        }
        #endregion


        #region Callbacks
        /// <summary>
        /// Called before the StateMachineBehaviours are enabled
        /// </summary>
        protected virtual void OnBeginEnterState() {
#if TLP_DEBUG
            DebugLog(nameof(OnBeginEnterState));
#endif
        }

        /// <summary>
        /// Called after the StateMachineBehaviours are enabled
        /// </summary>
        protected virtual void OnStateEntered() {
#if TLP_DEBUG
            DebugLog(nameof(OnStateEntered));
#endif
        }

        /// <summary>
        /// Called before the StateMachineBehaviours are disabled
        /// </summary>
        protected virtual void OnBeginExitState() {
#if TLP_DEBUG
            DebugLog(nameof(OnBeginExitState));
#endif
        }

        /// <summary>
        /// Called after the StateMachineBehaviours are disabled
        /// </summary>
        protected virtual void OnStateExited() {
#if TLP_DEBUG
            DebugLog(nameof(OnStateExited));
#endif
        }
        #endregion


        #region Public API
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nextStateMachineState"></param>
        /// <returns></returns>
        [PublicAPI]
        public bool TransitionTo(StateMachineState nextStateMachineState) {
            if (!Utilities.IsValid(nextStateMachineState)) {
#if TLP_DEBUG
                Error($"{nameof(TransitionTo)}: nextState invalid");
#endif
                return false;
            }

#if TLP_DEBUG
            DebugLog(
                    $"{nameof(TransitionTo)} {nextStateMachineState.GetUdonTypeName()} (index = {nextStateMachineState.StateMachineIndex})"
            );
#endif

            int nextStateIndex = nextStateMachineState.StateMachineIndex;
            if (nextStateIndex == StateMachineIndex) {
#if TLP_DEBUG
                Warn("transition to self is not supported");
#endif
                return false;
            }

            if (!Utilities.IsValid(StateMachine)) {
                Error(
                        "state machine invalid, make sure this state is assigned to it and the state machine has started successfully"
                );
                return false;
            }

            return StateMachine.OwnerSetNewState(nextStateIndex)
                   || StateMachine.LocalOnlySetNewState(nextStateIndex);
        }
        #endregion


        #region Internal
        private void EnableBehaviours() {
            if (StateMachineBehaviours == null) {
                return;
            }

            foreach (var entry in StateMachineBehaviours) {
                if (!Utilities.IsValid(entry)) {
                    continue;
                }

                entry.RelatedState = this;
                entry.enabled = true;
            }

            foreach (var entry in StateMachineBehaviours) {
                if (!Utilities.IsValid(entry)) {
                    continue;
                }

                entry.OnStateEntered();
            }
        }

        private void DisableBehaviours() {
            int lengthSafe = StateMachineBehaviours.LengthSafe();
            for (int i = lengthSafe - 1; i >= 0; i--) {
                var entry = StateMachineBehaviours[i];
                if (!Utilities.IsValid(entry)) {
                    continue;
                }

                entry.OnStateExited();
            }

            for (int i = lengthSafe - 1; i >= 0; i--) {
                var entry = StateMachineBehaviours[i];
                if (!Utilities.IsValid(entry)) {
                    continue;
                }

                entry.enabled = false;
                entry.RelatedState = null;
            }
        }
        #endregion
    }
}