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
                    ErrorAndDisableGameObject(
                            $"{nameof(GameObject)} {TlpLifeCycleExecutor.GlobalGameObjectName} not found");
                }
            }

            if (!Utilities.IsValid(_lifeCycleExecutor)) {
                ErrorAndDisableGameObject(
                        $"{nameof(GameObject)} {TlpLifeCycleExecutor.GlobalGameObjectName} with {GetUdonTypeName<TlpLifeCycleExecutor>()} not found");
                return;
            }

            _lifeCycleExecutor.Register(this);
        }

        internal void OnDisable() {
            if (!Utilities.IsValid(_lifeCycleExecutor)) return;
            _lifeCycleExecutor.Unregister(this);
        }

        public bool TlpIsAwake { get; internal set; }
        public bool TlpIsEnabled { get; internal set; }
        public bool TlpIsStarted { get; internal set; }

        internal void TlpWakeUp() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpAwake));
#endif
            #endregion

            TlpAwake();
            TlpIsAwake = true;
        }

        internal void TlpInit() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpInit));
#endif
            #endregion

            TlpStart();
            TlpIsStarted = true;
        }

        internal void TlpRunFixedUpdate() {
            TlpFixedUpdate();
        }

        internal void TlpRunUpdate() {
            TlpUpdate();
        }

        internal void TlpRunLateUpdate() {
            TlpLateUpdate();
        }

        internal void TlpRunPostLateUpdate() {
            TlpPostLateUpdate();
        }

        protected virtual void TlpAwake() {
        }

        protected virtual void TlpStart() {
        }

        protected virtual void TlpFixedUpdate() {
        }

        protected virtual void TlpUpdate() {
        }

        protected virtual void TlpLateUpdate() {
        }

        protected virtual void TlpPostLateUpdate() {
        }

        internal void TlpEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpEnable));
#endif
            #endregion

            TlpOnEnabled();
            TlpIsEnabled = true;
        }

        internal void TlpDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpDisable));
#endif
            #endregion

            TlpIsEnabled = false;
            TlpOnDisabled();
        }

        protected virtual void TlpOnEnabled() {
        }

        protected virtual void TlpOnDisabled() {
        }
    }
}