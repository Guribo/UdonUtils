using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using UnityEngine;
using VRC.SDKBase;

namespace Tlp.UdonUtils.Editor.Tests.Experimental.Executor
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TlpLifeCycleBehaviour), ExecutionOrder)]
    public abstract class TlpLifeCycleBehaviour : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpLifeCycleExecutor.ExecutionOrder + 1;
        #endregion



        private TlpLifeCycleExecutor _lifeCycleExecutor;


        internal void OnEnable() {
            if (!Utilities.IsValid(_lifeCycleExecutor)) {
                var lifeCycleExecutor = GameObject.Find(TlpLifeCycleExecutor.GlobalGameObjectName);
                if (Utilities.IsValid(lifeCycleExecutor)) {
                    _lifeCycleExecutor = lifeCycleExecutor.GetComponent<TlpLifeCycleExecutor>();
                } else {
                    _ErrorAndDisableGameObject(
                            $"{nameof(GameObject)} {TlpLifeCycleExecutor.GlobalGameObjectName} not found");
                }
            }

            if (!Utilities.IsValid(_lifeCycleExecutor)) {
                _ErrorAndDisableGameObject(
                        $"{nameof(GameObject)} {TlpLifeCycleExecutor.GlobalGameObjectName} with {GetUdonTypeName<TlpLifeCycleExecutor>()} not found");
                return;
            }

            _lifeCycleExecutor._Register(this);
        }

        internal void OnDisable() {
            if (!Utilities.IsValid(_lifeCycleExecutor)) return;
            _lifeCycleExecutor._Unregister(this);
        }

        public bool TlpIsAwake { get; private set; }
        public bool TlpIsEnabled { get; private set; }
        public bool TlpIsStarted { get; private set; }

        internal void _TlpWakeUp() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpAwake));
#endif
            #endregion

            _TlpAwake();
            TlpIsAwake = true;
        }

        internal void _TlpInit() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpInit));
#endif
            #endregion

            _TlpStart();
            TlpIsStarted = true;
        }

        internal void _TlpRunFixedUpdate() {
            _TlpFixedUpdate();
        }

        internal void _TlpRunUpdate() {
            _TlpUpdate();
        }

        internal void _TlpRunLateUpdate() {
            _TlpLateUpdate();
        }

        internal void _TlpRunPostLateUpdate() {
            _TlpPostLateUpdate();
        }

        protected virtual void _TlpAwake() {
        }

        protected virtual void _TlpStart() {
        }

        protected virtual void _TlpFixedUpdate() {
        }

        protected virtual void _TlpUpdate() {
        }

        protected virtual void _TlpLateUpdate() {
        }

        protected virtual void _TlpPostLateUpdate() {
        }

        internal void _TlpEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpEnable));
#endif
            #endregion

            _TlpOnEnabled();
            TlpIsEnabled = true;
        }

        internal void _TlpDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpDisable));
#endif
            #endregion

            TlpIsEnabled = false;
            _TlpOnDisabled();
        }

        protected virtual void _TlpOnEnabled() {
        }

        protected virtual void _TlpOnDisabled() {
        }
    }
}