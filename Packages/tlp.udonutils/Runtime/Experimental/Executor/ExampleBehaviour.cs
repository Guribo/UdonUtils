using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace Tlp.UdonUtils.Editor.Tests.Experimental.Executor
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ExampleBehaviour), ExecutionOrder)]
    public class ExampleBehaviour : TlpLifeCycleBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpLifeCycleBehaviour.ExecutionOrder + 1;
        #endregion

        protected override void TlpAwake() {
            base.TlpAwake();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpAwake));
#endif
            #endregion
        }

        protected override void TlpStart() {
            base.TlpStart();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpStart));
#endif
            #endregion
        }

        protected override void TlpFixedUpdate() {
            base.TlpFixedUpdate();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpFixedUpdate));
#endif
            #endregion
        }

        protected override void TlpUpdate() {
            base.TlpUpdate();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpUpdate));
#endif
            #endregion
        }

        protected override void TlpLateUpdate() {
            base.TlpLateUpdate();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpLateUpdate));
#endif
            #endregion
        }

        protected override void TlpPostLateUpdate() {
            base.TlpPostLateUpdate();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpPostLateUpdate));
#endif
            #endregion
        }

        protected override void TlpOnEnabled() {
            base.TlpOnEnabled();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpOnEnabled));
#endif
            #endregion
        }

        protected override void TlpOnDisabled() {
            base.TlpOnDisabled();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TlpOnDisabled));
#endif
            #endregion
        }
    }
}