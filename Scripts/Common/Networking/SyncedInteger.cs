using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace Guribo.UdonUtils.Scripts.Common.Networking
{
    /// <summary>
    /// Component which is used to synchronize a value on demand independently from high continuous synced udon
    /// behaviours to reduce bandwidth.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncedInteger : UdonSharpBehaviour
    {
        [UdonSynced]
        public int syncedValue = -1;

        private int _oldValue = -1;

        /// <summary>
        /// Udon behaviour that wants to have one of its variables synced to all players
        /// </summary>
        [SerializeField] protected UdonBehaviour targetBehaviour;
        /// <summary>
        /// Variable which will get synchronized with all players
        /// </summary>
        [SerializeField] protected string targetVariable = "mySyncedInteger";
        [Tooltip("Event to fire on all players when the value changes (instantly called on the owner)")]
        [SerializeField] protected string targetChangeEvent;
        [Tooltip("Event to fire on the owner when the value is about to be sent")]
        [SerializeField] protected string targetPreSerialization;
        [Tooltip("Event to fire on non-owning players when a value was received (can be the same value)")]
        [SerializeField] protected string targetDeserializeEvent;
        [Tooltip("Event to fire on the owner when the value was successfully sent")]
        [SerializeField] protected string targetSerializedEvent;

        /// <summary>
        /// Triggers Serialization of the manually synced player id.
        /// Does nothing if the caller does not own this behaviour/gameobject.
        /// </summary>
        /// <returns>false if the local player is not the owner or anything goes wrong</returns>
        public bool UpdateForAll()
        {
            var localPlayer = VRC.SDKBase.Networking.LocalPlayer;
            if (!Utilities.IsValid(targetBehaviour)
                || !Utilities.IsValid(localPlayer)
                || !localPlayer.IsOwner(gameObject))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(targetPreSerialization))
            {
                targetBehaviour.SendCustomEvent(targetPreSerialization);
            }

            var value = targetBehaviour.GetProgramVariable(targetVariable);
            if (value == null)
            {
                Debug.LogError(
                    $"SyncedInteger.UpdateForAll: '{targetVariable}' does not exist in '{targetBehaviour.name}'", this);
                return false;
            }

            // ReSharper disable once OperatorIsCanBeUsed
            if (value.GetType() == typeof(int))
            {
                syncedValue = (int) value;
                UpdateOldValueAndTriggerChangeEvent();
                RequestSerialization();

                return true;
            }

            Debug.LogError(
                $"SyncedInteger.UpdateForAll: '{targetVariable}' in '{targetBehaviour.name}' is not an integer", this);
            return false;
        }

        private void UpdateOldValueAndTriggerChangeEvent()
        {
            if (_oldValue != syncedValue)
            {
                _oldValue = syncedValue;
                if (!string.IsNullOrEmpty(targetChangeEvent))
                {
                    targetBehaviour.SendCustomEvent(targetChangeEvent);
                }
            }
        }

        public override void OnDeserialization()
        {
            var localPlayer = VRC.SDKBase.Networking.LocalPlayer;
            if (localPlayer.IsOwner(gameObject)
                || !Utilities.IsValid(targetBehaviour)
                || !Utilities.IsValid(localPlayer))
            {
                return;
            }

            // refresh the variable in the target udon behaviour
            targetBehaviour.SetProgramVariable(targetVariable, syncedValue);
            if (!string.IsNullOrEmpty(targetDeserializeEvent))
            {
                targetBehaviour.SendCustomEvent(targetDeserializeEvent);
            }

            UpdateOldValueAndTriggerChangeEvent();
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            var localPlayer = VRC.SDKBase.Networking.LocalPlayer;
            if (!(localPlayer.IsOwner(gameObject)
                  && Utilities.IsValid(targetBehaviour)
                  && Utilities.IsValid(localPlayer)))
            {
                Debug.LogWarning($"SyncedInteger.OnPostSerialization: aborting", this);
                return;
            }

            if (!result.success)
            {
                Debug.LogWarning($"SyncedInteger.OnPostSerialization: Serialization failed, trying again", this);
                RequestSerialization();
                return;
            }

            Debug.Log($"SyncedInteger.OnPostSerialization: Serialized {result.byteCount} bytes");

            if (!string.IsNullOrEmpty(targetSerializedEvent))
            {
                targetBehaviour.SendCustomEvent(targetSerializedEvent);
            }
        }
    }
}