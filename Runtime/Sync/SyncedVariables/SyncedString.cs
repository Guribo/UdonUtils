using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync.SyncedVariables
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncedString : UdonSharpBehaviour
    {
        [UdonSynced]
        [FieldChangeCallback(nameof(StringValueProperty))]
        internal string SyncedValue;

        public string[] targetFieldNames;
        public UdonSharpBehaviour[] listeners;

        public string StringValueProperty
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