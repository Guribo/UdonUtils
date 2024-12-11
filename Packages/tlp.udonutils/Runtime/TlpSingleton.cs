using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TlpSingleton), ExecutionOrder)]
    public class TlpSingleton : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpBaseBehaviour.ExecutionOrder + 1;
        #endregion

        [SerializeField]
        private string SingletonId;
        public string SingletonIdentifier => string.IsNullOrEmpty(SingletonId) ? GetUdonTypeName() : SingletonId;

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                Error("LocalPlayer is not valid");
                return false;
            }

            string identifier = SingletonIdentifier;
            if (string.IsNullOrEmpty(localPlayer.GetPlayerTag(identifier))) {
                localPlayer.SetPlayerTag(identifier, identifier);
            } else {
                Error("Singleton instance already exists/existed!");
                Destroy(gameObject);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves a component from a GameObject acting as a singleton.
        /// </summary>
        /// <param name="gameObjectName">GameObject to look for</param>
        /// <typeparam name="T">Component type to look for.</typeparam>
        /// <returns>null of the given gameobject name is not found,
        /// null if there is no <see cref="TlpSingleton"/> component on the GameObject,
        /// null if no component of the given type found on the GameObject.
        /// Otherwise, the component of the given type found on the GameObject.</returns>
        public static T GetInstance<T>(string gameObjectName) where T : UdonSharpBehaviour {
            var instance = GameObject.Find(gameObjectName);
            if (!Utilities.IsValid(instance)) {
                TlpLogger.StaticError($"GameObject '{gameObjectName}' not found", null);
                return null;
            }

            if (!Utilities.IsValid(instance.GetComponent<TlpSingleton>())) {
                TlpLogger.StaticError(
                        $"Component '{GetUdonTypeName<TlpSingleton>()}' not found on {instance.transform.GetPathInScene()}",
                        null,
                        instance);
                return null;
            }

            var tlpSingleton = instance.GetComponent<T>();
            if (Utilities.IsValid(tlpSingleton)) {
                return tlpSingleton;
            }

            TlpLogger.StaticError(
                    $"Component '{GetUdonTypeName<T>()}' not found on {instance.transform.GetPathInScene()}",
                    null,
                    instance);
            return null;
        }
    }
}