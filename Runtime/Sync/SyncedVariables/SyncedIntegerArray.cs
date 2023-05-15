using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync.SyncedVariables
{
    /// <summary>
    /// Component which is used to synchronize a value on demand independently from high continuous synced udon
    /// behaviours to reduce bandwidth.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncedIntegerArray : TlpBaseBehaviour
    {
        [UdonSynced]
        public int[] syncedValue;

        [HideInInspector]
        public int[] oldValue;

        #region Target behaviour

        [Header("Target settings")]
        /// <summary>
        /// Udon behaviour that wants to have one of its variables synced to all players
        /// </summary>
        public UdonSharpBehaviour targetBehaviour;

        /// <summary>
        /// Variable which will get synchronized with all players
        /// </summary>
        public string targetVariable = "values";

        #endregion

        #region Optional Callbacks

        [Header("Optional callback events")]
        public UdonSharpBehaviour[] changeEventListeners;

        [Tooltip("Event to fire on all players when the value changes (instantly called on the owner)")]
        public string targetChangeEvent;

        public UdonSharpBehaviour[] preSerializationEventListeners;

        [Tooltip("Event to fire on the owner when the value is about to be sent")]
        public string targetPreSerialization;

        public UdonSharpBehaviour[] deserializeEventListeners;

        [Tooltip("Event to fire on non-owning players when a value was received (can be the same value)")]
        public string targetDeserializeEvent;

        public UdonSharpBehaviour[] serializedEventListeners;

        [Tooltip("Event to fire on the owner when the value was successfully sent")]
        public string targetSerializedEvent;

        #endregion

        /// <summary>
        /// Triggers Serialization of the manually synced player id.
        /// Does nothing if the caller does not own this behaviour/gameobject.
        /// </summary>
        /// <returns>false if the local player is not the owner or anything goes wrong</returns>
        public bool UpdateForAll()
        {
#if TLP_DEBUG
            DebugLog(nameof(UpdateForAll));
#endif

            var localPlayer = Networking.LocalPlayer;

            if (!Assert(Utilities.IsValid(localPlayer), "Local player invalid", this)
                || !Assert(
                    Utilities.IsValid(localPlayer.IsOwner(gameObject)),
                    "Local player is not owner",
                    this
                )
                || !Assert(Utilities.IsValid(targetBehaviour), "Target UdonBehaviour invalid", this))
            {
                return false;
            }

            Notify(preSerializationEventListeners, targetPreSerialization);

            object value = targetBehaviour.GetProgramVariable(targetVariable);
            if (value != null)
            {
                int[] array = (int[])value;
                syncedValue = new int[array.Length];
                array.CopyTo(syncedValue, 0);
            }
            else
            {
                syncedValue = null;
            }

            UpdateOldValueAndTriggerChangeEvent();
#if TLP_DEBUG
            DebugLog(nameof(RequestSerialization));
#endif
            RequestSerialization();

            return true;
        }

        private void UpdateOldValueAndTriggerChangeEvent()
        {
            Notify(changeEventListeners, targetChangeEvent);

            if (syncedValue != null)
            {
                int[] temp = new int[syncedValue.Length];
                syncedValue.CopyTo(temp, 0);
                oldValue = temp;
            }
            else
            {
                oldValue = null;
            }
        }

        public override void OnDeserialization()
        {
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer.IsOwner(gameObject)
                || !Utilities.IsValid(targetBehaviour)
                || !Utilities.IsValid(localPlayer))
            {
                return;
            }

            // refresh the variable in the target udon behaviour
            targetBehaviour.SetProgramVariable(targetVariable, syncedValue);
            Notify(deserializeEventListeners, targetDeserializeEvent);

            UpdateOldValueAndTriggerChangeEvent();
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            var localPlayer = Networking.LocalPlayer;
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

            Notify(serializedEventListeners, targetSerializedEvent);
        }

        internal void Notify(UdonSharpBehaviour[] listeners, string eventName)
        {
#if TLP_DEBUG
            DebugLog(nameof(Notify));
#endif
            if (listeners != null && !string.IsNullOrEmpty(eventName))
            {
                foreach (var preSerializationEventListener in listeners)
                {
                    if (Utilities.IsValid(preSerializationEventListener))
                    {
                        preSerializationEventListener.SendCustomEvent(eventName);
                    }
                }
            }
        }
    }
}