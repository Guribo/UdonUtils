using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Sync.SyncedVariables
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [Obsolete("Use sync Events instead")]
    public class SyncedBool : UdonSharpBehaviour
    {
        [UdonSynced]
        [FieldChangeCallback(nameof(BoolValueProperty))]
        internal bool SyncedValue;

        public string[] targetFieldNames;
        public UdonSharpBehaviour[] listeners;

        public bool BoolValueProperty
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


        internal bool ListenerSetupValid()
        {
            bool listenersNull = listeners == null;
            if (listenersNull)
            {
                return false;
            }

            bool targetVariablesNull = targetFieldNames == null;
            if (targetVariablesNull)
            {
                return false;
            }

            return listeners.Length == targetFieldNames.Length;
        }

        internal void NotifyListeners()
        {
            if (!ListenerSetupValid())
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