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

        protected override void _TlpAwake() {
            base._TlpAwake();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpAwake));
#endif
            #endregion
        }

        protected override void _TlpStart() {
            base._TlpStart();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpStart));
#endif
            #endregion
        }

        protected override void _TlpFixedUpdate() {
            base._TlpFixedUpdate();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpFixedUpdate));
#endif
            #endregion
        }

        protected override void _TlpUpdate() {
            base._TlpUpdate();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpUpdate));
#endif
            #endregion
        }

        protected override void _TlpLateUpdate() {
            base._TlpLateUpdate();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpLateUpdate));
#endif
            #endregion
        }

        protected override void _TlpPostLateUpdate() {
            base._TlpPostLateUpdate();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpPostLateUpdate));
#endif
            #endregion
        }

        protected override void _TlpOnEnabled() {
            base._TlpOnEnabled();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpOnEnabled));
#endif
            #endregion
        }

        protected override void _TlpOnDisabled() {
            base._TlpOnDisabled();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TlpOnDisabled));
#endif
            #endregion
        }
    }
}