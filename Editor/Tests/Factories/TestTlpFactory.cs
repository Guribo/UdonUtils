using System;
using NUnit.Framework;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Factories;
using TLP.UdonUtils.Tests.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TLP.UdonUtils.Tests.Editor.Factories
{
    [TestFixture(Category = "TLP/UdonUtils")]
    public class TestTlpFactory : TestWithLogger
    {
        private Transform _factoryRoot;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _factoryRoot = new GameObject(TlpFactory.FactoriesGameObjectName).transform;
        }

        [Test]
        public void TestGetConcreteFactory()
        {
            // Arrange
            var factoryGameObject = new GameObject("TestFactory");
            factoryGameObject.AddComponent<TestFactory>();
            factoryGameObject.transform.parent = _factoryRoot;

            // Act
            var factory = TlpFactory.GetConcreteFactory<TestFactory>(null);

            // Assert
            Assert.IsNotNull(factory);
            Assert.AreEqual(factory, factoryGameObject.GetComponent<TestFactory>());

            // Clean up
            Object.DestroyImmediate(factoryGameObject);
        }

        [Test]
        public void DoesNotFindFactoriesNotAttachedToGlobalGameObject()
        {
            // Arrange
            var factoryGameObject = new GameObject("TestFactory");
            factoryGameObject.AddComponent<TestFactory>();

            // Assert
            ExpectError(
                $"Factory GameObject '{nameof(TestFactory)}' with a '{nameof(TestFactory)}' component was not found"
            );
            var factory = TlpFactory.GetConcreteFactory<TestFactory>(null);
            Assert.False(factory);
        }

        [Test]
        public void TestGetConcreteFactoryWithConcreteProduct()
        {
            // Arrange
            var factoryGameObject = new GameObject("TestFactory");
            var createdFactory = factoryGameObject.AddComponent<InstantiatingFactory>();
            createdFactory.transform.parent = _factoryRoot;
            createdFactory.Prototype = createdFactory.gameObject.AddComponent<UdonEvent>();
            createdFactory.OnEnable();

            // Act
            var factory = TlpFactory.GetConcreteFactory<InstantiatingFactory>(nameof(UdonEvent));

            // Assert
            Assert.IsNotNull(factory);
            Assert.AreEqual(factory, createdFactory);

            // Clean up
            Object.DestroyImmediate(factoryGameObject);
        }

        [Test]
        public void TestCreateInstance()
        {
            // Arrange
            var factoryGameObject = new GameObject("TestFactory");
            var factory = factoryGameObject.AddComponent<TestFactory>();
            factory.transform.parent = _factoryRoot;

            // Act
            var instance = factory.CreateInstance();

            // Assert
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.name, "TestInstance");

            // Clean up
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(factoryGameObject);
        }

        [Test]
        public void TestCreateInstanceWithConcreteProduct()
        {
            // Arrange
            var factoryGameObject = new GameObject("TestFactory");
            var createdFactory = factoryGameObject.AddComponent<InstantiatingFactory>();
            createdFactory.transform.parent = _factoryRoot;
            createdFactory.Prototype = createdFactory.gameObject.AddComponent<UdonEvent>();
            createdFactory.OnEnable();

            // Act
            var factory = TlpFactory.GetConcreteFactory<InstantiatingFactory>(nameof(UdonEvent));
            var instance = factory.CreateInstance();

            // Assert
            Assert.IsNotNull(factory);
            Assert.AreEqual(factory.gameObject.name, "InstantiatingFactory_for_type_UdonEvent");
            Assert.AreNotEqual(factory, instance);

            Assert.AreEqual(instance.name, "InstantiatingFactory_for_type_UdonEvent(Clone)");
            Assert.IsNotNull(instance.GetComponent<InstantiatingFactory>());
            Assert.IsNotNull(instance.GetComponent<UdonEvent>());

            // Clean up
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(factoryGameObject);
        }
    }
}