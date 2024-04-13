using TLP.UdonUtils.Factories;
using UnityEngine;

namespace TLP.UdonUtils.Tests
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