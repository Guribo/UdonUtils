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
    public class SyncedInteger : UdonSharpBehaviour
    {
        /// <summary>
        /// resets the value back to 0 after it was successfully sent
        /// </summary>
        public bool autoResetOnSuccess;

        [UdonSynced]
        [FieldChangeCallback(nameof(IntValueProperty))]
        internal int SyncedValue;

        public string[] targetFieldNames;
        public UdonSharpBehaviour[] listeners;

        public int IntValueProperty
        {
            set
            {
                bool valueUnchanged = SyncedValue == value;
                if (valueUnchanged)
                {
                    return;
                }

                SyncedValue = value;

                if (Networking.IsOwner(gameObject))
                {
                    RequestSerialization();
                }

                NotifyListeners();
            }
            get => SyncedValue;
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (result.success)
            {
                if (autoResetOnSuccess)
                {
                    IntValueProperty = 0;
                }

                return;
            }

            SendCustomEventDelayedSeconds(nameof(RequestSerialization), 1f);
        }

        internal void NotifyListeners()
        {
            bool listenersInvalid = listeners == null
                                    || targetFieldNames == null
                                    || listeners.Length != targetFieldNames.Length;
            if (listenersInvalid)
            {
                Debug.LogError("Invalid listener setup");
                return;
            }

            for (int i = 0; i < listeners.Length; i++)
            {
                if (Utilities.IsValid(listeners[i]))
                {
                    listeners[i].SetProgramVariable(targetFieldNames[i], SyncedValue);
                }
            }
        }
    }
}