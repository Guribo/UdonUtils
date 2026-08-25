using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRCStation = VRC.SDKBase.VRCStation;

namespace TLP.UdonUtils.Runtime.Sync
{
    public static class OwnershipTransfer
    {
        /// <summary>
        /// Changes the ownership of the entire hierarchy of the GameObject provided, including all relevant parents
        /// and their children
        /// </summary>
        /// <param name="go"></param>
        /// <param name="newOwner"></param>
        /// <param name="requireSuccess">If set to true it is checked that we are the owner after ownership transfer,
        /// if transfer was denied then false is returned.
        /// If set to false the result of the ownership transfer is ignored and true is returned</param>
        /// <returns>True on success or false of transfer failed or is incomplete</returns>
        public static bool _TransferOwnership(GameObject go, VRCPlayerApi newOwner, bool requireTransferSuccess) {
            return _TransferOwnershipFrom(go, null, newOwner, requireTransferSuccess);
        }

        public static bool _TransferOwnershipFrom(
                GameObject go,
                GameObject start,
                VRCPlayerApi newOwner,
                bool requireTransferSuccess
        ) {
            if (!Utilities.IsValid(go)) {
                Debug.LogError($"{nameof(_TransferOwnership)}: Invalid gameobject");
                return !requireTransferSuccess;
            }

            if (!Utilities.IsValid(newOwner)) {
                Debug.LogError($"{nameof(_TransferOwnership)}: Invalid new owner");
                return !requireTransferSuccess;
            }

            Component topBehaviour;
            if (!Utilities.IsValid(start)) {
                topBehaviour = UdonCommon._FindTopComponent(typeof(Transform), go.transform);
                if (!Utilities.IsValid(topBehaviour)) {
                    topBehaviour = go.transform;
                }
            } else {
                topBehaviour = start.transform;
            }

            var allTransforms = topBehaviour.transform.GetComponentsInChildren<Transform>(true);

            if (allTransforms == null || allTransforms.Length == 0) {
                Debug.LogError(
                        $"{nameof(_TransferOwnership)}:  GameObject {go.name} " +
                        $"has no udon behaviours it its hierarchy"
                );
                return false;
            }

            bool anyFailures = false;
            int newOwnerPlayerId = newOwner.playerId;

            foreach (var childTransform in allTransforms) {
                if (!Utilities.IsValid(childTransform)) {
                    Debug.LogWarning($"{nameof(_TransferOwnership)}:  invalid transform found. Skipping.");
                    continue;
                }

                var childGo = childTransform.gameObject;
                // make sure to not overload the network by only taking ownership of objects that have synced components
                if (Utilities.IsValid(childGo.GetComponent(typeof(UdonBehaviour)))
                    || Utilities.IsValid(childGo.GetComponent(typeof(UdonSharpBehaviour)))
                    || Utilities.IsValid(childGo.GetComponent(typeof(VRCStation)))
                    || Utilities.IsValid(childGo.GetComponent(typeof(VRC_Pickup)))
                    || Utilities.IsValid(childGo.GetComponent(typeof(VRCObjectSync)))) {
                    int oldOwnerId = -1;
                    var oldOwner = Networking.GetOwner(childTransform.gameObject);
                    if (Utilities.IsValid(oldOwner)) {
                        oldOwnerId = oldOwner.playerId;
                    }

                    Debug.Log(
                            $"{nameof(_TransferOwnership)}:  setting owner of " +
                            $"'{childTransform.gameObject.name}' " +
                            $"from player {oldOwnerId} to player {newOwnerPlayerId}"
                    );

                    if (!Networking.IsOwner(childTransform.gameObject)) {
                        Networking.SetOwner(newOwner, childTransform.gameObject);

                        // check, if required whether we are the owner now
                        if (requireTransferSuccess && !Networking.IsOwner(childTransform.gameObject)) {
                            anyFailures = true;
                        }
                    }
                }
            }

            return !requireTransferSuccess || !anyFailures;
        }

        public static bool _TakeOwnership(this UdonSharpBehaviour behaviour) {
            return Utilities.IsValid(behaviour) && behaviour.gameObject._TakeOwnership();
        }

        public static bool _TakeOwnership(this GameObject gameObject) {
            if (!Utilities.IsValid(gameObject)) {
                return false;
            }

            var vrcPlayerApi = Networking.LocalPlayer;
            if (Networking.IsOwner(vrcPlayerApi, gameObject)) {
                return true;
            }

            Networking.SetOwner(vrcPlayerApi, gameObject);
            return Networking.IsOwner(vrcPlayerApi, gameObject);
        }

        /// <summary>
        /// If the current owner is not the master it iterates through the list of player in O(n) time to find the
        /// master and transfer ownership
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns>false if ownership transfer was not allowed by the behaviour or arguments are invalid,
        /// true if already owner or ownership transfer succeeded</returns>
        public static bool _TransferOwnershipToMaster(this TlpBaseBehaviour behaviour) {
            if (!Utilities.IsValid(behaviour)) {
                Debug.LogError($"{nameof(_TransferOwnershipToMaster)}: {nameof(behaviour)} is invalid");
                return false;
            }

            if (Networking.GetOwner(behaviour.gameObject)._IsMasterSafe()) {
                // nothing to do
                return true;
            }

            var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            players = VRCPlayerApi.GetPlayers(players);
            foreach (var existingPlayer in players) {
                if (!existingPlayer._IsMasterSafe()) {
                    continue;
                }

                if (Networking.IsOwner(existingPlayer, behaviour.gameObject)) {
                    return true;
                }

                Networking.SetOwner(existingPlayer, behaviour.gameObject);
                return Networking.IsOwner(existingPlayer, behaviour.gameObject);
            }

            return false;
        }
    }
}