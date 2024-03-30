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
                    : $"{factoryTypeName}_for_type_{productTypeName}";
        }

        private int _failureCount;

        public void Start() {
#if TLP_DEBUG
            DebugLog(nameof(Start));
#endif

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

        public static T GetConcreteFactory<T>(string keyOrUdonTypeName) where T : TlpFactory {
            string typeName = keyOrUdonTypeName == null ? null : UdonCommon.UdonTypeNameShort(keyOrUdonTypeName);
            string factoryTypeName = UdonCommon.UdonTypeNameShort(GetUdonTypeName<T>());
            string nameToFind = typeName == null
                    ? factoryTypeName
                    : $"{factoryTypeName}_for_type_{typeName}";

#if TLP_DEBUG
            Debug.Log($"{nameof(GetConcreteFactory)} '{nameToFind}'");
#endif
            if (!FindFactoriesGameObject(out var runtimeFactories)) {
                return null;
            }

            var allFactories = runtimeFactories.transform.GetComponentsInChildren<T>(true);
            foreach (var factory in allFactories) {
                if (!Utilities.IsValid(factory)) {
                    continue;
                }

                if (factory.gameObject.name.Equals(nameToFind, StringComparison.InvariantCultureIgnoreCase)) {
                    return factory;
                }
            }

            Debug.LogError(
                    $"Factory GameObject '{nameToFind}' with a '{factoryTypeName}' component was not found"
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