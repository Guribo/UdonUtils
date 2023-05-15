using TLP.UdonUtils.Runtime.Sync.SyncedVariables;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonUtils.Tests.Runtime
{
    public class TestSyncedBool : UdonSharpBehaviour
    {
        [FormerlySerializedAs("exampleToggleTarget")]
        public GameObject ExampleToggleTarget;

        [FormerlySerializedAs("syncedBool")]
        [SerializeField]
        internal SyncedBool SyncedBool;

        [FormerlySerializedAs("regularBoolField")]
        [FieldChangeCallback(nameof(RegularBoolProperty))]
        public bool RegularBoolField;

        public bool RegularBoolProperty
        {
            set
            {
                bool valueUnchanged = RegularBoolField == value;
                if (valueUnchanged)
                {
                    Debug.Log("SyncedRegularBool unchanged");
                    return;
                }

                Debug.Log($"SyncedRegularBool changed from {RegularBoolField} to {value}");
                RegularBoolField = value;
                RegularBoolChanged();

                if (!(Utilities.IsValid(SyncedBool)
                      && Networking.IsOwner(gameObject)))
                {
                    return;
                }

                if (!Networking.IsOwner(SyncedBool.gameObject))
                {
                    Networking.SetOwner(Networking.LocalPlayer, SyncedBool.gameObject);
                }

                SyncedBool.BoolValueProperty = value;
            }
            get => RegularBoolField;
        }

        internal void RegularBoolChanged()
        {
            ExampleToggleTarget.SetActive(RegularBoolProperty);
        }

        public override void Interact()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RegularBoolProperty = !RegularBoolProperty;
        }
    }
}