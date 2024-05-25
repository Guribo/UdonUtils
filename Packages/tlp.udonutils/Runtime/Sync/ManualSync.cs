using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync
{
    [Obsolete("Use NetworkTransform intead")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ManualSync : UdonSharpBehaviour
    {
        public float updateInterval = 0.25f;
        public float lastNetworkUpdate;

        [UdonSynced]
        public Vector3 position;

        [UdonSynced]
        public Vector3 rotation;

        public Rigidbody ownRigidbody;

        private void Start() {
            lastNetworkUpdate = Time.unscaledTime;
            if (Networking.IsMaster) {
                TakeOwnership();
            }
        }

        public void LateUpdate() {
            if (!Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
                return;
            }

            float unscaledTime = Time.unscaledTime;
            if (unscaledTime - lastNetworkUpdate > updateInterval) {
                lastNetworkUpdate = unscaledTime;
                var transform1 = transform;
                position = transform1.position;
                rotation = transform1.rotation.eulerAngles;
                RequestSerialization();
            }
        }

        public override void OnPickup() {
            TakeOwnership();
        }

        public override void Interact() {
            TakeOwnership();
            var vrcPlayerApi = Networking.LocalPlayer;
            if (Utilities.IsValid(vrcPlayerApi)) {
                vrcPlayerApi.UseAttachedStation();
            }
        }

        private void TakeOwnership() {
            var localPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(localPlayer)) {
                Networking.SetOwner(localPlayer, gameObject);
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            if (!Utilities.IsValid(player)) {
                Debug.LogWarning(
                        GetLogPrefix() + $"ManualSync.OnOwnershipTransferred: Ownership transferred " +
                        $"to invalid player"
                );
                return;
            }

            Debug.Log(
                    GetLogPrefix() + $"ManualSync.OnOwnershipTransferred: Ownership transferred to " +
                    $"{player.displayName} ({player.playerId})"
            );
        }

        public override void OnPreSerialization() {
            Debug.Log(GetLogPrefix() + $"ManualSync.OnPreSerialization: ");
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            Debug.Log(GetLogPrefix() + $"ManualSync.OnDeserialization: ");

            if (Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
                return;
            }

            if (Utilities.IsValid(ownRigidbody)) {
                ownRigidbody.angularVelocity = Vector3.zero;
                ownRigidbody.velocity = Vector3.zero;
            }

            transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
        }

        public override void OnPostSerialization(SerializationResult result) {
            if (!result.success) {
                Debug.LogWarning(
                        GetLogPrefix() + $"ManualSync.OnPostSerialization: Serialization failed, trying again",
                        this
                );
                RequestSerialization();
                return;
            }

            Debug.Log(GetLogPrefix() + $"ManualSync.OnPostSerialization: Serialized {result.byteCount} bytes");
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner) {
            bool requester = false;
            bool requested = false;
            if (!Utilities.IsValid(requestingPlayer)) {
                Debug.LogWarning(GetLogPrefix() + $"ManualSync.OnOwnershipRequest: requesting player is invalid");
            } else {
                Debug.Log(
                        GetLogPrefix() + $"ManualSync.OnOwnershipRequest: " +
                        $"{requestingPlayer.displayName} ({requestingPlayer.playerId}) requests ownership change"
                );
                requester = true;
            }

            if (!Utilities.IsValid(requestedOwner)) {
                Debug.LogWarning(GetLogPrefix() + $"ManualSync.OnOwnershipRequest: requested owner is invalid");
            } else {
                Debug.Log(
                        GetLogPrefix() + $"ManualSync.OnOwnershipRequest: " +
                        $"{requestedOwner.displayName} ({requestedOwner.playerId}) is requested to be owner"
                );
                requested = true;
            }

            Debug.Log(
                    GetLogPrefix() + $"ManualSync.OnOwnershipRequest: " +
                    $"Granting ownership transfer: {requester && requested}"
            );
            return requester && requested;
        }

        private string GetLogPrefix() {
            var player = Networking.LocalPlayer;
            if (Utilities.IsValid(player)) {
                return $"[{player.displayName} ({player.playerId})] ";
            }

            return "[Unknown local player] ";
        }
    }
}