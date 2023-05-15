using TLP.UdonUtils.Runtime.Factories;
using UnityEngine;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonUtils.Tests.Runtime
{
    public class TestFactory : TlpFactory
    {
        protected override GameObject ProduceInstance()
        {
            var instance = Instantiate(gameObject);
            DestroyImmediate(instance.gameObject.GetComponent<TestFactory>());
            instance.name = "TestInstance";
            return instance;
        }
    }
}