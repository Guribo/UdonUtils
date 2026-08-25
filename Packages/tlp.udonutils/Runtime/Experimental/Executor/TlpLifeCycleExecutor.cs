using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Experimental.Tasks;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace Tlp.UdonUtils.Editor.Tests.Experimental.Executor
{
    public enum LifeCycleState
    {
        None,
        Enable,
        Awake,
        Start,
        FixedUpdate,
        Update,
        LateUpdate,
        PostLateUpdate,
        Disable,
        Destroy
    }


    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TlpLifeCycleExecutor), ExecutionOrder)]
    public class TlpLifeCycleExecutor : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = Task.ExecutionOrder + 50;
        #endregion

        public const string GlobalGameObjectName = "Tlp_LifeCycleExecutor";

        #region State
        private LifeCycleState _lifeCycleState = LifeCycleState.None;

        private readonly DataList _executionOrder = new DataList();
        private readonly DataDictionary _executionOrderLookupTable = new DataDictionary();

        #region Currently Processed Order
        private int _executionOrderProcessing;

        /// <summary>
        /// may be updated if there was new behaviours inserted after current order <see cref="_executionOrderProcessing"/>
        /// </summary>
        private DataList _growingOnlyOrder;

        private bool _growingOnlyOutdated;
        private bool _growingOnlyOutdatedOnNewFrame;
        private int _refreshRequestFrame;
        #endregion
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            _growingOnlyOrder = _executionOrder.DeepClone();
            _lifeCycleState = LifeCycleState.Awake;
            for (int i = 0; i < _growingOnlyOrder.Count; ++i) {
                var behaviour = (TlpLifeCycleBehaviour)_growingOnlyOrder[i].Reference;
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                if (behaviour.enabled) {
                    _executionOrderProcessing = behaviour.ExecutionOrderReadOnly;
                    behaviour._TlpWakeUp();
                }
            }

            _lifeCycleState = LifeCycleState.Enable;
            _RefreshGrowingOrder();

            for (int i = 0; i < _growingOnlyOrder.Count; ++i) {
                var behaviour = (TlpLifeCycleBehaviour)_growingOnlyOrder[i].Reference;
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                if (behaviour.enabled) {
                    _executionOrderProcessing = behaviour.ExecutionOrderReadOnly;
                    behaviour._TlpEnable();
                }
            }

            _lifeCycleState = LifeCycleState.Start;
            _RefreshGrowingOrder();
            for (int i = 0; i < _growingOnlyOrder.Count; ++i) {
                var behaviour = (TlpLifeCycleBehaviour)_growingOnlyOrder[i].Reference;
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                if (behaviour.TlpIsEnabled) {
                    _executionOrderProcessing = behaviour.ExecutionOrderReadOnly;
                    behaviour._TlpInit();
                }
            }

            return true;
        }
        #endregion

        #region VRChat LifeCycle
        internal void FixedUpdate() {
            _lifeCycleState = LifeCycleState.FixedUpdate;
            if (_growingOnlyOutdatedOnNewFrame && Time.frameCount == _refreshRequestFrame) {
                _growingOnlyOutdatedOnNewFrame = false;
                _growingOnlyOutdated = false;
                _growingOnlyOrder = _executionOrder.DeepClone();
            }

            for (int i = 0; i < _growingOnlyOrder.Count; ++i) {
                var behaviour = (TlpLifeCycleBehaviour)_growingOnlyOrder[i].Reference;
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                if (behaviour.TlpIsEnabled) {
                    _executionOrderProcessing = behaviour.ExecutionOrderReadOnly;
                    behaviour._TlpRunFixedUpdate();
                }
            }
        }


        internal void Update() {
            _lifeCycleState = LifeCycleState.Update;
            for (int i = 0; i < _growingOnlyOrder.Count; ++i) {
                var behaviour = (TlpLifeCycleBehaviour)_growingOnlyOrder[i].Reference;
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                if (behaviour.TlpIsEnabled) {
                    _executionOrderProcessing = behaviour.ExecutionOrderReadOnly;
                    behaviour._TlpRunUpdate();
                }
            }
        }

        internal void LateUpdate() {
            _lifeCycleState = LifeCycleState.LateUpdate;
            for (int i = 0; i < _growingOnlyOrder.Count; ++i) {
                var behaviour = (TlpLifeCycleBehaviour)_growingOnlyOrder[i].Reference;
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                if (behaviour.TlpIsEnabled) {
                    _executionOrderProcessing = behaviour.ExecutionOrderReadOnly;
                    behaviour._TlpRunLateUpdate();
                }
            }
        }

        public override void PostLateUpdate() {
            base.PostLateUpdate();
            _lifeCycleState = LifeCycleState.PostLateUpdate;
            for (int i = 0; i < _growingOnlyOrder.Count; ++i) {
                var behaviour = (TlpLifeCycleBehaviour)_growingOnlyOrder[i].Reference;
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                if (behaviour.TlpIsEnabled) {
                    _executionOrderProcessing = behaviour.ExecutionOrderReadOnly;
                    behaviour._TlpRunPostLateUpdate();
                }
            }
        }
        #endregion

        #region Internal
        internal void _Register(TlpLifeCycleBehaviour tlpLifeCycleBehaviour) {
            if (!Utilities.IsValid(tlpLifeCycleBehaviour)) {
                _Error($"{nameof(_Register)}: Invalid {nameof(tlpLifeCycleBehaviour)}");
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(
                    $"{nameof(_Register)}: {tlpLifeCycleBehaviour._GetScriptPathInScene()} {tlpLifeCycleBehaviour.ExecutionOrderReadOnly}");
#endif
            #endregion

            _executionOrder.Clear();

            if (_executionOrderLookupTable.TryGetValue(tlpLifeCycleBehaviour.ExecutionOrderReadOnly, out var entries)) {
                var behaviours = (DataList)entries;
                int i = 0;
                for (; i < behaviours.Count; i++) {
                    var reference = (TlpLifeCycleBehaviour)behaviours[i].Reference;
                    if (string.Compare(
                                reference.GetUdonTypeName(),
                                tlpLifeCycleBehaviour.GetUdonTypeName(),
                                StringComparison.Ordinal) <= 0 && string.Compare(
                                reference._GetScriptPathInScene(),
                                tlpLifeCycleBehaviour._GetScriptPathInScene(),
                                StringComparison.Ordinal) <= 0) {
                        continue;
                    }

                    break;
                }

                behaviours.Insert(i, tlpLifeCycleBehaviour);
            } else {
                var behaviours = new DataList();
                behaviours.Add(tlpLifeCycleBehaviour);
                _executionOrderLookupTable.Add(tlpLifeCycleBehaviour.ExecutionOrderReadOnly, behaviours);
            }

            _RegenerateExecutionOrder();
            _InitRegisteredBehaviour(tlpLifeCycleBehaviour);
        }

        internal void _Unregister(TlpLifeCycleBehaviour tlpLifeCycleBehaviour) {
            if (!Utilities.IsValid(tlpLifeCycleBehaviour)) {
                _Error($"{nameof(_Unregister)}: Invalid {nameof(tlpLifeCycleBehaviour)}");
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(
                    $"{nameof(_Unregister)}: {tlpLifeCycleBehaviour._GetScriptPathInScene()} {tlpLifeCycleBehaviour.ExecutionOrderReadOnly}");
#endif
            #endregion

            if (!_executionOrderLookupTable.TryGetValue(
                        tlpLifeCycleBehaviour.ExecutionOrderReadOnly,
                        out var entries)) {
                return;
            }

            var behaviours = (DataList)entries;
            behaviours.Remove(tlpLifeCycleBehaviour);
            if (behaviours.Count == 0) {
                _executionOrderLookupTable.Remove(tlpLifeCycleBehaviour.ExecutionOrderReadOnly);
            }

            _growingOnlyOutdatedOnNewFrame = true;
            _refreshRequestFrame = Time.frameCount + 1;
        }
        #endregion

        #region Private
        private void _InitRegisteredBehaviour(TlpLifeCycleBehaviour tlpLifeCycleBehaviour) {
            switch (_lifeCycleState) {
                case LifeCycleState.None:
                    break;
                case LifeCycleState.Awake:
                    if (tlpLifeCycleBehaviour.ExecutionOrderReadOnly <= _executionOrderProcessing) {
                        if (!tlpLifeCycleBehaviour.TlpIsAwake) {
                            tlpLifeCycleBehaviour._TlpWakeUp();
                        }
                    }

                    _growingOnlyOutdated = true;
                    break;
                case LifeCycleState.Enable:
                    if (!tlpLifeCycleBehaviour.TlpIsAwake) {
                        tlpLifeCycleBehaviour._TlpWakeUp();
                    }

                    if (tlpLifeCycleBehaviour.ExecutionOrderReadOnly <= _executionOrderProcessing) {
                        if (!tlpLifeCycleBehaviour.TlpIsEnabled) {
                            tlpLifeCycleBehaviour._TlpEnable();
                        }
                    }

                    _growingOnlyOutdated = true;

                    break;
                case LifeCycleState.Start:
                    if (!tlpLifeCycleBehaviour.TlpIsAwake) {
                        tlpLifeCycleBehaviour._TlpWakeUp();
                    }

                    if (!tlpLifeCycleBehaviour.TlpIsEnabled) {
                        tlpLifeCycleBehaviour._TlpEnable();
                    }

                    if (tlpLifeCycleBehaviour.ExecutionOrderReadOnly <= _executionOrderProcessing) {
                        if (!tlpLifeCycleBehaviour.TlpIsStarted) {
                            tlpLifeCycleBehaviour._TlpInit();
                        }
                    }

                    _growingOnlyOutdated = true;
                    break;
                case LifeCycleState.FixedUpdate:
                case LifeCycleState.Update:
                case LifeCycleState.LateUpdate:
                case LifeCycleState.PostLateUpdate:
                    if (!tlpLifeCycleBehaviour.TlpIsAwake) {
                        tlpLifeCycleBehaviour._TlpWakeUp();
                    }

                    if (!tlpLifeCycleBehaviour.TlpIsEnabled) {
                        tlpLifeCycleBehaviour._TlpEnable();
                    }

                    if (!tlpLifeCycleBehaviour.TlpIsStarted) {
                        tlpLifeCycleBehaviour._TlpInit();
                    }

                    _growingOnlyOutdatedOnNewFrame = true;
                    _refreshRequestFrame = Time.frameCount + 1;
                    break;
                case LifeCycleState.Disable:
                    break;
                case LifeCycleState.Destroy:
                    break;
                default:
                    break;
            }
        }

        private void _RegenerateExecutionOrder() {
            var keys = _executionOrderLookupTable.GetKeys();
            keys.Sort();
            for (int i = 0; i < keys.Count; i++) {
                _executionOrderProcessing = keys[i].Int;
                _DebugLog($"ExecutionOrder: {_executionOrderProcessing}");
                var behaviours = _executionOrderLookupTable[_executionOrderProcessing].DataList;
                for (int j = 0; j < behaviours.Count; j++) {
                    if (!Utilities.IsValid(behaviours[j].Reference)) continue;
                    _DebugLog($"{((TlpLifeCycleBehaviour)behaviours[j].Reference)._GetScriptPathInScene()}");
                    _executionOrder.Add(behaviours[j]);
                }
            }
        }

        private void _RefreshGrowingOrder() {
            if (!_growingOnlyOutdated) {
                return;
            }

            _growingOnlyOutdated = false;
            _growingOnlyOrder = _executionOrder.DeepClone();
        }
        #endregion
    }
}