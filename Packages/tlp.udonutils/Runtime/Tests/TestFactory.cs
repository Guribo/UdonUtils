using TLP.UdonUtils.Runtime.Factories;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Tests
{
    public class TestFactory : TlpFactory
    {
        protected override GameObject ProduceInstance() {
            var instance = Instantiate(gameObject);
            DestroyImmediate(instance.gameObject.GetComponent<TestFactory>());
            instance.name = "TestInstance";
            return instance;
        }
    }
}