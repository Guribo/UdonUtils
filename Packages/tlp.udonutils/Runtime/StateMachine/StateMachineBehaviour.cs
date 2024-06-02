﻿using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.StateMachine
{
    /// <summary>
    /// Can be used to implement behaviours that should run in a given state.
    /// Only active while the <see cref="RelatedState"/> is set and active.
    ///
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class StateMachineBehaviour : TlpBaseBehaviour
    {
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = StateMachineState.ExecutionOrder + 1;
        #endregion

        #region State
        /// <summary>
        /// Initialized by the state currently using this behaviour before this behaviour is being enabled, is set to
        /// null when no longer used by the state after this behaviour is disabled
        /// </summary>
        [NonSerialized]
        public StateMachineState RelatedState;
        #endregion

        #region Callbacks
        /// <summary>
        /// Called after <see cref="StateMachineState.OnBeginEnterState"/>.
        /// Called after all behaviours of the state have been enabled.
        /// Called before <see cref="StateMachineState.OnStateEntered"/>.
        /// </summary>
        public abstract void OnStateEntered();

        /// <summary>
        /// Called after <see cref="StateMachineState.OnBeginExitState"/>.
        /// Called before all behaviours of the state have been disabled.
        /// Called before <see cref="StateMachineState.OnStateExited"/>.
        /// Called in reverse order compared to <see cref="OnStateEntered"/> if there
        /// is multiple behaviours on one state.
        /// </summary>
        public abstract void OnStateExited();
        #endregion
    }
}