using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Common;
using TLP.UdonUtils.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Factories
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public abstract class TlpFactory : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DefaultStart;

        public const string FactoriesGameObjectName = "TLP_Factories";

        private bool _initialized;

        [Tooltip("Key of the factory when trying to access it globally")]
        public string FactoryKey;

        protected virtual string GetProductTypeName() {
            return FactoryKey;
        }

        public void OnEnable() {
#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif

            string productTypeName = GetProductTypeName();
            string factoryTypeName = UdonCommon.UdonTypeNameShort(GetUdonTypeName());
            gameObject.name = string.IsNullOrWhiteSpace(productTypeName)
                    ? factoryTypeName
                    : $"{factoryTypeName}_of_product_{productTypeName}";
        }

        private int _failureCount;

        public override void Start() {
            base.Start();

            if (_initialized) {
                return;
            }

            if (!InitializeFactory()) {
                if (_failureCount >= 3) {
                    Error("Initialization failed completely");
                    enabled = false;
                    return;
                }

                ++_failureCount;
                Warn($"Initialization of {gameObject.name} failed {_failureCount} times");
                SendCustomEventDelayedFrames(nameof(Start), 1);
                return;
            }

            _initialized = true;
#if TLP_DEBUG
            if (Severity == ELogLevel.Debug) {
                DebugLog("Creating test instance");
                CreateInstance();
            }
#endif
        }

        public static T GetConcreteFactory<T>(string productKeyOrUdonTypeName) where T : TlpFactory {
            string productTypeName = productKeyOrUdonTypeName == null ? null : UdonCommon.UdonTypeNameShort(productKeyOrUdonTypeName);
            string factoryTypeName = UdonCommon.UdonTypeNameShort(GetUdonTypeName<T>());
            string gameObjectToFind = productTypeName == null
                    ? factoryTypeName
                    : $"{factoryTypeName}_of_product_{productTypeName}";

#if TLP_DEBUG
            Debug.Log($"{nameof(GetConcreteFactory)} '{gameObjectToFind}'");
#endif
            if (!FindFactoriesGameObject(out var runtimeFactories)) {
                return null;
            }

            var allFactories = runtimeFactories.transform.GetComponentsInChildren<T>(true);
            foreach (var factory in allFactories) {
                if (!Utilities.IsValid(factory)) {
                    continue;
                }

                if (factory.gameObject.name.Equals(gameObjectToFind, StringComparison.InvariantCultureIgnoreCase)) {
                    return factory;
                }
            }

            Debug.LogError(
                    $"GetConcreteFactory '{gameObjectToFind}' with a '{factoryTypeName}' component for product '{productTypeName}' was not found"
            );
            return null;
        }

        protected static bool FindFactoriesGameObject(out GameObject runtimeFactories) {
            runtimeFactories = GameObject.Find(FactoriesGameObjectName);
            if (!Utilities.IsValid(runtimeFactories)) {
                Debug.LogError(
                        $"GameObject called '{FactoriesGameObjectName}' not found or not valid, ensure it exists and is active and a child of '{FactoriesGameObjectName}'"
                );
                return false;
            }

            return true;
        }


        protected virtual bool InitializeFactory() {
            return true;
        }

        public GameObject CreateInstance() {
#if TLP_DEBUG
            DebugLog(nameof(CreateInstance));
#endif
            if (!enabled) {
                Warn("Can not produce when not enabled");
                return null;
            }

            if (!_initialized) {
                if (!InitializeFactory()) {
                    Error("Initialization failed");
                    return null;
                }

                _initialized = true;
            }

            return ProduceInstance();
        }

        protected abstract GameObject ProduceInstance();
    }
}