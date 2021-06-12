using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;


namespace Guribo.UdonUtils.Scripts.Common.Networking
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class OwnershipTransfer : UdonSharpBehaviour
    {
        #region Libraries

        [Header("Libraries")]
        public UdonCommon udonCommon;
        public UdonDebug udonDebug;

        #endregion


        /// <summary>
        /// Changes the ownership of the entire hierarchy of the gameobject provided, including all relevant parents
        /// and their children
        /// </summary>
        /// <param name="go"></param>
        /// <param name="newOwner"></param>
        /// <param name="requireSuccess">If set to true it is checked that we are the owner after ownership transfer,
        /// if transfer was denied then false is returned.
        /// If set to false the result of the ownership transfer is ignored and true is returned</param>
        /// <returns>True on success or false of transfer failed or is incomplete</returns>
        public bool TransferOwnership(GameObject go, VRCPlayerApi newOwner, bool requireTransferSuccess)
        {
            if(!(udonDebug.Assert(Utilities.IsValid(go),"OwnershipTransfer.SetOwner: GameObject invalid", this)
             && udonDebug.Assert(Utilities.IsValid(newOwner),"OwnershipTransfer.SetOwner: new owner invalid", this)))
            {
                return false;
            }

            // find the top most udon behaviour
            var topBehaviour = udonCommon.FindTopComponent(typeof(UdonBehaviour), go.transform);
            if (!Utilities.IsValid(topBehaviour))
            {
                Debug.LogError($"OwnershipTransfer.SetOwner: GameObject {go.name} " +
                               $"has no parent udon behaviour which could change ownership");
                return false;
            }

            var allTransforms = topBehaviour.transform.GetComponentsInChildren<Transform>(true);

            if (allTransforms == null || allTransforms.Length == 0)
            {
                Debug.LogError($"OwnershipTransfer.SetOwner: GameObject {go.name} " +
                               $"has no udon behaviours it its hierarchy");
                return false;
            }

            var anyFailures = false;
            var newOwnerPlayerId = newOwner.playerId;

            foreach (var childTransform in allTransforms)
            {
                if (!Utilities.IsValid(childTransform))
                {
                    Debug.LogWarning("OwnershipTransfer.SetOwner: invalid transform found. Skipping.");
                    continue;
                }

                var childGo = childTransform.gameObject;
                // make sure to not overload the network by only taking ownership of objects that have synced components
                if (Utilities.IsValid(childGo.GetComponent(typeof(UdonBehaviour)))
                    || Utilities.IsValid(childGo.GetComponent(typeof(VRC.SDKBase.VRCStation)))
                    || Utilities.IsValid(childGo.GetComponent(typeof(VRC_Pickup)))
                    || Utilities.IsValid(childGo.GetComponent(typeof(VRCObjectSync))))
                {
                    var oldOwnerId = -1;
                    var oldOwner = VRC.SDKBase.Networking.GetOwner(childTransform.gameObject);
                    if (Utilities.IsValid(oldOwner))
                    {
                        oldOwnerId = oldOwner.playerId;
                    }

                    Debug.Log($"OwnershipTransfer.SetOwner: setting owner of " +
                              $"'{childTransform.gameObject.name}' " +
                              $"from player {oldOwnerId} to player {newOwnerPlayerId}");

                    if (!VRC.SDKBase.Networking.IsOwner(childTransform.gameObject))
                    {
                        VRC.SDKBase.Networking.SetOwner(newOwner, childTransform.gameObject);

                        // check, if required whether we are the owner now
                        if (requireTransferSuccess && !VRC.SDKBase.Networking.IsOwner(childTransform.gameObject))
                        {
                            anyFailures = true;
                        }
                    }
                }
            }

            return !requireTransferSuccess || !anyFailures;
        }
    }
}