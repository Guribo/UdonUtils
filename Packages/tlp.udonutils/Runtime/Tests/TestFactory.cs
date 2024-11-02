using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Factories;
using TLP.UdonUtils.Runtime.Pool;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Tests
{
    public class TestFactory : TlpFactory
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = FactoryWithPool.ExecutionOrder + 1;

        protected override GameObject ProduceInstance() {
            var instance = Instantiate(gameObject);
            DestroyImmediate(instance.gameObject.GetComponent<TestFactory>());
            instance.name = "TestInstance";
            return instance;
        }
    }
}