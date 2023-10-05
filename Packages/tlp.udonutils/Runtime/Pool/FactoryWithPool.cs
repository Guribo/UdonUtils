using JetBrains.Annotations;
using TLP.UdonUtils.Common;
using TLP.UdonUtils.Factories;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonPool.Runtime
{
    [DefaultExecutionOrder(ExecutionOrder)]
    public class FactoryWithPool : InstantiatingFactory
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = InstantiatingFactory.ExecutionOrder + 1;

        private InstantiatingFactory _poolFactory;
        private Pool _productPool;
        public Pool ProductPool => _productPool;


        public Pool OptionalDefaultPool;

        protected override bool InitializeFactory()
        {
            if (Utilities.IsValid(OptionalDefaultPool))
            {
                DebugLog($"Using {nameof(OptionalDefaultPool)}");
                _productPool = OptionalDefaultPool;
                return true;
            }

            if (!Utilities.IsValid(Prototype))
            {
                Error($"{gameObject.name}: {nameof(Prototype)} is not set");
                return false;
            }

            if (!AttachToRuntimeFactoriesGameObject())
            {
                return false;
            }

            _poolFactory = GetConcreteFactory<FactoryWithPool>(nameof(Pool));
            if (!Utilities.IsValid(_poolFactory))
            {
                Error($"{gameObject.name}: {nameof(FactoryWithPool)} with key {nameof(ProductPool)} not found");
                return false;
            }

            var poolGameObject = _poolFactory.CreateInstance();
            if (!poolGameObject)
            {
                return false;
            }

            _productPool = poolGameObject.GetComponent<Pool>();
            if (!Utilities.IsValid(_productPool))
            {
                Error($"Created {nameof(poolGameObject)} has no {nameof(ProductPool)} attached");
                Destroy(poolGameObject);
                return false;
            }

            _productPool.PoolInstancePrefab = Prototype.gameObject;
            _productPool.gameObject.name =
                $"{nameof(ProductPool)}_of_{UdonCommon.UdonTypeNameShort(Prototype.GetUdonTypeName())}s";

            if (ConfigurePool(_productPool))
            {
                poolGameObject.SetActive(true);
                return true;
            }

            Error("Failed to configure pool");
            Destroy(poolGameObject);
            return false;
        }

        private bool AttachToRuntimeFactoriesGameObject()
        {
            if (!FindFactoriesGameObject(out var runtimeFactories))
            {
                return false;
            }

            transform.parent = runtimeFactories.transform;
            return true;
        }

        protected virtual bool ConfigurePool(Pool pool)
        {
            return true;
        }

        protected override GameObject ProduceInstance()
        {
            if (!Utilities.IsValid(_productPool))
            {
                Error($"{nameof(ProductPool)} invalid");
                return null;
            }

            return _productPool.Get();
        }
    }
}