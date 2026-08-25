using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Factories;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Pool
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(FactoryWithPool), ExecutionOrder)]
    public class FactoryWithPool : InstantiatingFactory
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = InstantiatingFactory.ExecutionOrder + 1;


        #region Dependencies
        [SerializeField]
        internal InstantiatingFactory PoolFactory;
        #endregion
        private Pool _productPool;

        public Pool OptionalDefaultPool;

        #region Internal
        private bool _AttachToRuntimeFactoriesGameObject() {
            if (!_FindFactoriesGameObject(out var runtimeFactories)) {
                return false;
            }

            transform.parent = runtimeFactories.transform;
            return true;
        }
        #endregion


        #region Overrides
        protected override bool _InitializeFactory() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_InitializeFactory));
#endif
#endregion
            if (!Utilities.IsValid(Prototype)) {
                _Error($"{gameObject.name}: {nameof(Prototype)} is not set");
                return false;
            }

            if (Utilities.IsValid(OptionalDefaultPool)) {
                _DebugLog($"Using {nameof(OptionalDefaultPool)}");
                _productPool = OptionalDefaultPool;
                return true;
            }

            if (!_AttachToRuntimeFactoriesGameObject()) {
                return false;
            }

            if (!Utilities.IsValid(PoolFactory)) {
                _Error($"{nameof(PoolFactory)} not set");
                return false;
            }

            var poolGameObject = PoolFactory._CreateInstance();
            if (!poolGameObject) {
                return false;
            }

            _productPool = poolGameObject.GetComponent<Pool>();
            if (!Utilities.IsValid(_productPool)) {
                _Error($"Created {nameof(poolGameObject)} has no {nameof(Pool)} attached");
                Destroy(poolGameObject);
                return false;
            }

            _productPool.PoolInstancePrefab = Prototype.gameObject;
            _productPool.gameObject.name =
                    $"{nameof(Pool)}_of_{UdonCommon._UdonTypeNameShort(Prototype.GetUdonTypeName())}s";

            if (_ConfigurePool(_productPool)) {
                poolGameObject.SetActive(true);
                return true;
            }

            _Error("Failed to configure pool");
            return false;
        }

        protected virtual bool _ConfigurePool(Pool pool) {
            return true;
        }
        #endregion


        #region Public
        protected override GameObject _ProduceInstance() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_ProduceInstance));
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(_ProduceInstance)}: Not initialized");
                return null;
            }

            return _productPool._Get();
        }

        public void _Return(GameObject instance) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Return));
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(_Return)}: Not initialized");
                return;
            }

            _productPool._Return(instance);
        }
        #endregion
    }
}