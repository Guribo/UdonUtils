using System;
using TLP.UdonUtils.Events;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Sync.SyncedVariables
{
    /// <summary>
    /// Component which is used to synchronize a value on demand independently from high continuous synced udon
    /// behaviours to reduce bandwidth.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [Obsolete("Use sync Events instead", false)]
    public class SyncedInteger : TlpBaseBehaviour
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
        public UdonEvent onChanged;

        public int IntValueProperty
        {
            set
            {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Set IntValueProperty: {value}");
#endif
                #endregion


                bool valueUnchanged = SyncedValue == value;
                if (valueUnchanged) {
                    return;
                }

                SyncedValue = value;

                MarkNetworkDirty();
                RequestSerialization();
                NotifyListeners();
            }
            get => SyncedValue;
        }

        public override void OnPostSerialization(SerializationResult result) {
            base.OnPostSerialization(result);
            if (!result.success) {
                return;
            }

            if (autoResetOnSuccess) {
                IntValueProperty = 0;
            }
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);
            NotifyListeners();
        }

        internal void NotifyListeners() {
            if (Utilities.IsValid(onChanged)) {
                onChanged.Raise(this);
            }

            bool listenersInvalid = listeners == null
                                    || targetFieldNames == null
                                    || listeners.Length != targetFieldNames.Length;
            if (listenersInvalid) {
                Debug.LogError("Invalid listener setup");
                return;
            }

            for (int i = 0; i < listeners.Length; i++) {
                if (!Utilities.IsValid(listeners[i])) {
                    Warn($"Invalid listener at index %{i}");
                    continue;
                }

                listeners[i].SetProgramVariable(targetFieldNames[i], SyncedValue);
            }
        }
    }
}