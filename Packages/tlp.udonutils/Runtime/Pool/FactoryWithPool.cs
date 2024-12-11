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
        private bool AttachToRuntimeFactoriesGameObject() {
            if (!FindFactoriesGameObject(out var runtimeFactories)) {
                return false;
            }

            transform.parent = runtimeFactories.transform;
            return true;
        }
        #endregion


        #region Overrides

        protected override bool InitializeFactory() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(InitializeFactory));
#endif
#endregion
            if (!Utilities.IsValid(Prototype)) {
                Error($"{gameObject.name}: {nameof(Prototype)} is not set");
                return false;
            }

            if (Utilities.IsValid(OptionalDefaultPool)) {
                DebugLog($"Using {nameof(OptionalDefaultPool)}");
                _productPool = OptionalDefaultPool;
                return true;
            }

            if (!AttachToRuntimeFactoriesGameObject()) {
                return false;
            }

            if (!Utilities.IsValid(PoolFactory)) {
                Error($"{nameof(PoolFactory)} not set");
                return false;
            }

            var poolGameObject = PoolFactory.CreateInstance();
            if (!poolGameObject) {
                return false;
            }

            _productPool = poolGameObject.GetComponent<Pool>();
            if (!Utilities.IsValid(_productPool)) {
                Error($"Created {nameof(poolGameObject)} has no {nameof(Pool)} attached");
                Destroy(poolGameObject);
                return false;
            }

            _productPool.PoolInstancePrefab = Prototype.gameObject;
            _productPool.gameObject.name =
                    $"{nameof(Pool)}_of_{UdonCommon.UdonTypeNameShort(Prototype.GetUdonTypeName())}s";

            if (ConfigurePool(_productPool)) {
                poolGameObject.SetActive(true);
                return true;
            }

            Error("Failed to configure pool");
            return false;
        }

        protected virtual bool ConfigurePool(Pool pool) {
            return true;
        }
        #endregion


        #region Public
        protected override GameObject ProduceInstance() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(ProduceInstance));
#endif
            #endregion

            if (!HasStartedOk) {
                Error($"{nameof(ProduceInstance)}: Not initialized");
                return null;
            }

            return _productPool.Get();
        }

        public void Return(GameObject instance) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Return));
#endif
            #endregion

            if (!HasStartedOk) {
                Error($"{nameof(Return)}: Not initialized");
                return;
            }

            _productPool.Return(instance);
        }
        #endregion
    }
}