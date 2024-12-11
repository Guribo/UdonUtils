using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Experimental.Tasks;
using TLP.UdonUtils.Runtime.Sources.Time;
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
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
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
                    behaviour.TlpWakeUp();
                }
            }

            _lifeCycleState = LifeCycleState.Enable;
            RefreshGrowingOrder();

            for (int i = 0; i < _growingOnlyOrder.Count; ++i) {
                var behaviour = (TlpLifeCycleBehaviour)_growingOnlyOrder[i].Reference;
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                if (behaviour.enabled) {
                    _executionOrderProcessing = behaviour.ExecutionOrderReadOnly;
                    behaviour.TlpEnable();
                }
            }

            _lifeCycleState = LifeCycleState.Start;
            RefreshGrowingOrder();
            for (int i = 0; i < _growingOnlyOrder.Count; ++i) {
                var behaviour = (TlpLifeCycleBehaviour)_growingOnlyOrder[i].Reference;
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                if (behaviour.TlpIsEnabled) {
                    _executionOrderProcessing = behaviour.ExecutionOrderReadOnly;
                    behaviour.TlpInit();
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
                    behaviour.TlpRunFixedUpdate();
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
                    behaviour.TlpRunUpdate();
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
                    behaviour.TlpRunLateUpdate();
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
                    behaviour.TlpRunPostLateUpdate();
                }
            }
        }
        #endregion

        #region Internal
        internal void Register(TlpLifeCycleBehaviour tlpLifeCycleBehaviour) {
            if (!Utilities.IsValid(tlpLifeCycleBehaviour)) {
                Error($"{nameof(Register)}: Invalid {nameof(tlpLifeCycleBehaviour)}");
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(
                    $"{nameof(Register)}: {tlpLifeCycleBehaviour.GetScriptPathInScene()} {tlpLifeCycleBehaviour.ExecutionOrderReadOnly}");
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
                                reference.GetScriptPathInScene(),
                                tlpLifeCycleBehaviour.GetScriptPathInScene(),
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

            RegenerateExecutionOrder();
            InitRegisteredBehaviour(tlpLifeCycleBehaviour);
        }

        internal void Unregister(TlpLifeCycleBehaviour tlpLifeCycleBehaviour) {
            if (!Utilities.IsValid(tlpLifeCycleBehaviour)) {
                Error($"{nameof(Unregister)}: Invalid {nameof(tlpLifeCycleBehaviour)}");
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(
                    $"{nameof(Unregister)}: {tlpLifeCycleBehaviour.GetScriptPathInScene()} {tlpLifeCycleBehaviour.ExecutionOrderReadOnly}");
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
        private void InitRegisteredBehaviour(TlpLifeCycleBehaviour tlpLifeCycleBehaviour) {
            switch (_lifeCycleState) {
                case LifeCycleState.None:
                    break;
                case LifeCycleState.Awake:
                    if (tlpLifeCycleBehaviour.ExecutionOrderReadOnly <= _executionOrderProcessing) {
                        if (!tlpLifeCycleBehaviour.TlpIsAwake) {
                            tlpLifeCycleBehaviour.TlpWakeUp();
                        }
                    }

                    _growingOnlyOutdated = true;
                    break;
                case LifeCycleState.Enable:
                    if (!tlpLifeCycleBehaviour.TlpIsAwake) {
                        tlpLifeCycleBehaviour.TlpWakeUp();
                    }

                    if (tlpLifeCycleBehaviour.ExecutionOrderReadOnly <= _executionOrderProcessing) {
                        if (!tlpLifeCycleBehaviour.TlpIsEnabled) {
                            tlpLifeCycleBehaviour.TlpEnable();
                        }
                    }

                    _growingOnlyOutdated = true;

                    break;
                case LifeCycleState.Start:
                    if (!tlpLifeCycleBehaviour.TlpIsAwake) {
                        tlpLifeCycleBehaviour.TlpWakeUp();
                    }

                    if (!tlpLifeCycleBehaviour.TlpIsEnabled) {
                        tlpLifeCycleBehaviour.TlpEnable();
                    }

                    if (tlpLifeCycleBehaviour.ExecutionOrderReadOnly <= _executionOrderProcessing) {
                        if (!tlpLifeCycleBehaviour.TlpIsStarted) {
                            tlpLifeCycleBehaviour.TlpInit();
                        }
                    }

                    _growingOnlyOutdated = true;
                    break;
                case LifeCycleState.FixedUpdate:
                case LifeCycleState.Update:
                case LifeCycleState.LateUpdate:
                case LifeCycleState.PostLateUpdate:
                    if (!tlpLifeCycleBehaviour.TlpIsAwake) {
                        tlpLifeCycleBehaviour.TlpWakeUp();
                    }

                    if (!tlpLifeCycleBehaviour.TlpIsEnabled) {
                        tlpLifeCycleBehaviour.TlpEnable();
                    }

                    if (!tlpLifeCycleBehaviour.TlpIsStarted) {
                        tlpLifeCycleBehaviour.TlpInit();
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

        private void RegenerateExecutionOrder() {
            var keys = _executionOrderLookupTable.GetKeys();
            keys.Sort();
            for (int i = 0; i < keys.Count; i++) {
                _executionOrderProcessing = keys[i].Int;
                DebugLog($"ExecutionOrder: {_executionOrderProcessing}");
                var behaviours = _executionOrderLookupTable[_executionOrderProcessing].DataList;
                for (int j = 0; j < behaviours.Count; j++) {
                    if (!Utilities.IsValid(behaviours[j].Reference)) continue;
                    DebugLog($"{((TlpLifeCycleBehaviour)behaviours[j].Reference).GetScriptPathInScene()}");
                    _executionOrder.Add(behaviours[j]);
                }
            }
        }

        private void RefreshGrowingOrder() {
            if (!_growingOnlyOutdated) {
                return;
            }

            _growingOnlyOutdated = false;
            _growingOnlyOrder = _executionOrder.DeepClone();
        }
        #endregion
    }
}