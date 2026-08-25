using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Logger;
using TLP.UdonUtils.Runtime.Physics;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Factories
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TlpFactory), ExecutionOrder)]
    public abstract class TlpFactory : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = InertiaTensor.ExecutionOrder + 1;

        public const string FactoriesGameObjectName = "TLP_Factories";

        [Tooltip("Key of the factory when trying to access it globally")]
        public string FactoryKey;

        protected virtual string _GetProductTypeName() {
            return FactoryKey;
        }

        internal void OnEnable() {
#if TLP_DEBUG
            _DebugLog(nameof(OnEnable));
#endif

            string productTypeName = _GetProductTypeName();
            string factoryTypeName = UdonCommon._UdonTypeNameShort(GetUdonTypeName());
            gameObject.name = string.IsNullOrWhiteSpace(productTypeName)
                    ? factoryTypeName
                    : $"{factoryTypeName}_of_product_{productTypeName}";
        }

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_InitializeFactory()) {
                _Error($"{nameof(_InitializeFactory)} failed");
                return false;
            }

            return true;
        }

        public static T _GetConcreteFactory<T>(string productKeyOrUdonTypeName) where T : TlpFactory {
            string productTypeName = productKeyOrUdonTypeName == null
                    ? null
                    : UdonCommon._UdonTypeNameShort(productKeyOrUdonTypeName);
            string factoryTypeName = UdonCommon._UdonTypeNameShort(GetUdonTypeName<T>());
            string gameObjectToFind = productTypeName == null
                    ? factoryTypeName
                    : $"{factoryTypeName}_of_product_{productTypeName}";

#if TLP_DEBUG
            TlpLogger.StaticDebugLog($"{nameof(_GetConcreteFactory)} '{gameObjectToFind}'", null);
#endif
            if (!_FindFactoriesGameObject(out var runtimeFactories)) {
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

            TlpLogger.StaticError(
                    $"GetConcreteFactory '{gameObjectToFind}' with a '{factoryTypeName}' component for product '{productTypeName}' was not found",
                    null);
            return null;
        }

        protected static bool _FindFactoriesGameObject(out GameObject runtimeFactories) {
            runtimeFactories = GameObject.Find(FactoriesGameObjectName);
            if (!Utilities.IsValid(runtimeFactories)) {
                TlpLogger.StaticError(
                        $"GameObject called '{FactoriesGameObjectName}' not found or not valid, ensure it exists and is active and a child of '{FactoriesGameObjectName}'",
                        null);
                return false;
            }

            return true;
        }


        protected virtual bool _InitializeFactory() {
            return true;
        }

        public GameObject _CreateInstance() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_CreateInstance));
#endif
            #endregion

            if (!enabled) {
                _Warn("Can not produce when not enabled");
                return null;
            }

            if (HasStartedOk) {
                return _ProduceInstance();
            }

            _Error("Not initialized");
            return null;
        }

        protected abstract GameObject _ProduceInstance();
    }
}