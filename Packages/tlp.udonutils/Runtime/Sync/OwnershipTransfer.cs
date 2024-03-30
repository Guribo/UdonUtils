using TLP.UdonUtils.Common;
using TLP.UdonUtils.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRCStation = VRC.SDKBase.VRCStation;

namespace TLP.UdonUtils.Sync
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
        public static bool TransferOwnership(GameObject go, VRCPlayerApi newOwner, bool requireTransferSuccess) {
            return TransferOwnershipFrom(go, null, newOwner, requireTransferSuccess);
        }

        public static bool TransferOwnershipFrom(
                GameObject go,
                GameObject start,
                VRCPlayerApi newOwner,
                bool requireTransferSuccess
        ) {
            if (!Utilities.IsValid(go)) {
                Debug.LogError($"{nameof(TransferOwnership)}: Invalid gameobject");
                return !requireTransferSuccess;
            }

            if (!Utilities.IsValid(newOwner)) {
                Debug.LogError($"{nameof(TransferOwnership)}: Invalid new owner");
                return !requireTransferSuccess;
            }

            Component topBehaviour;
            if (!Utilities.IsValid(start)) {
                // find the top most udon behaviour
                topBehaviour = UdonCommon.FindTopComponent(typeof(UdonBehaviour), go.transform);
                if (!Utilities.IsValid(topBehaviour)) {
                    Debug.LogWarning(
                            $"{nameof(TransferOwnership)}: GameObject {go.name} " +
                            $"has no parent UdonBehaviour which could change ownership"
                    );

                    topBehaviour = UdonCommon.FindTopComponent(typeof(UdonSharpBehaviour), go.transform);
                    if (!Utilities.IsValid(topBehaviour)) {
                        Debug.LogError(
                                $"{nameof(TransferOwnership)}: GameObject {go.name} " +
                                $"also has no parent UdonSharpBehaviour which could change ownership"
                        );
                        return false;
                    }
                }
            } else {
                topBehaviour = start.transform;
            }


            var allTransforms = topBehaviour.transform.GetComponentsInChildren<Transform>(true);

            if (allTransforms == null || allTransforms.Length == 0) {
                Debug.LogError(
                        $"{nameof(TransferOwnership)}:  GameObject {go.name} " +
                        $"has no udon behaviours it its hierarchy"
                );
                return false;
            }

            bool anyFailures = false;
            int newOwnerPlayerId = newOwner.playerId;

            foreach (var childTransform in allTransforms) {
                if (!Utilities.IsValid(childTransform)) {
                    Debug.LogWarning($"{nameof(TransferOwnership)}:  invalid transform found. Skipping.");
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
                            $"{nameof(TransferOwnership)}:  setting owner of " +
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

        public static bool TakeOwnership(this UdonSharpBehaviour behaviour) {
            return Utilities.IsValid(behaviour) && behaviour.gameObject.TakeOwnership();
        }

        public static bool TakeOwnership(this GameObject gameObject) {
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
        public static bool TransferOwnershipToMaster(this TlpBaseBehaviour behaviour) {
            if (!Utilities.IsValid(behaviour)) {
                Debug.LogError($"{nameof(TransferOwnershipToMaster)}: {nameof(behaviour)} is invalid");
                return false;
            }

            if (Networking.GetOwner(behaviour.gameObject).IsMasterSafe()) {
                // nothing to do
                return true;
            }

            var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            players = VRCPlayerApi.GetPlayers(players);
            foreach (var existingPlayer in players) {
                if (!existingPlayer.IsMasterSafe()) {
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