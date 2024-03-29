﻿using JetBrains.Annotations;
using TLP.UdonUtils.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonUtils.Common
{
    /// <summary>
    /// 1. Notifies other behaviours when a player with an old/new version of the current world
    ///    enters the current instance.
    /// 2. Notifies other behaviours and players when the local player enters the world and has
    ///    a new/old version of that world.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class WorldVersionCheck : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpLogger.ExecutionOrder + 1;

        [FormerlySerializedAs("syncedBuild")]
        [UdonSynced]
        public int SyncedBuild;

        [FormerlySerializedAs("syncedInstanceCompromised")]
        [UdonSynced]
        [HideInInspector]
        public bool SyncedInstanceCompromised;

        [FormerlySerializedAs("syncedTotalVersionConflicts")]
        [UdonSynced]
        [HideInInspector]
        public int SyncedTotalVersionConflicts;

        internal bool InstanceCompromised;
        internal bool LocalPlayerSkipUpdateNotification;
        internal int LocalVersionConflicts;

        [FormerlySerializedAs("timestamp")]
        public long Timestamp;

        [Tooltip("Behaviours to notify when a player with a new world version joins")]
        public UdonSharpBehaviour[] updateAvailableListeners;

        [Tooltip("Name of the custom event to call on the behaviours in updateAvailableListeners")]
        public string updateAvailableEvent = "WorldUpdateAvailable";

        [Tooltip("Behaviours to notify when a world version conflict between a joining player and the master occurs")]
        public UdonSharpBehaviour[] versionConflictListeners;

        [Tooltip("Name of the custom event to call on the behaviours in versionConflictListeners")]
        public string versionConflictEvent = "VersionConflictOccurred";

        [Header("Auto generated/updated on upload")]
        [Tooltip("Is automatically incremented during upload, can be set to an initial value")]
        public int build;

        internal void Start()
        {
            Info($"Build: {build} Timestamp: {Timestamp}");

            if (VRC.SDKBase.Networking.IsMaster)
            {
                SyncedBuild = build;
                RequestSerialization();
            }
            else
            {
                CheckBuildRecency();
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player)
                && VRC.SDKBase.Networking.IsMaster
                && player.isLocal)
            {
                SyncedBuild = build;
                RequestSerialization();
            }
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            return VRC.SDKBase.Networking.IsMaster;
        }

        public override void OnDeserialization(DeserializationResult deserializationResult)
        {
            bool versionConflictsAlreadyKnown = InstanceCompromised;
            if (versionConflictsAlreadyKnown)
            {
                Warn(
                    $"There was {SyncedTotalVersionConflicts} version conflicts in this instance " +
                    $"({LocalVersionConflicts} conflicts were detected locally). " +
                    $"Consider creating a new instance."
                );
                return;
            }

            CheckBuildRecency();
        }

        internal void LocalVersionOutOfDate()
        {
            Warn(
                $"The world is {SyncedBuild - build} builds out of date! Please clear your VRChat cache " +
                $"and restart the game."
            );
            NotifyListeners(updateAvailableListeners, updateAvailableEvent);
        }

        internal void CheckBuildRecency()
        {
            bool localPlayerNeedsToUpdate = SyncedBuild > build;
            if (localPlayerNeedsToUpdate)
            {
                LocalVersionOutOfDate();
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RPC_OutOfDatePlayerJoined));
                return;
            }

            bool worldUpdateAvailable = build > SyncedBuild;
            if (worldUpdateAvailable)
            {
                LocalPlayerSkipUpdateNotification = true;
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RPC_VersionUpdateAvailable));
                return;
            }

            if (SyncedInstanceCompromised)
            {
                HandleVersionConflict(false);
            }
        }

        internal void HandleVersionConflict(bool locallyDetected)
        {
            InstanceCompromised = true;

            if (locallyDetected)
            {
                LocalVersionConflicts++;
            }

            if (VRC.SDKBase.Networking.IsMaster)
            {
                if (locallyDetected)
                {
                    ++SyncedTotalVersionConflicts;
                }

                SyncedInstanceCompromised = true;
                RequestSerialization();
            }


            Warn(
                $"There was {SyncedTotalVersionConflicts} version conflicts in this instance " +
                $"({LocalVersionConflicts} conflicts were detected locally). " +
                $"Consider creating a new instance."
            );

            NotifyListeners(versionConflictListeners, versionConflictEvent);
        }

        #region Listener Notifying

        internal bool ListenerSetupValid(UdonSharpBehaviour[] listeners, string target)
        {
            bool listenersNull = listeners == null;
            if (listenersNull)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(target);
        }

        internal void NotifyListeners(UdonSharpBehaviour[] listeners, string target)
        {
            if (!ListenerSetupValid(listeners, target))
            {
                Error($"Invalid listener setup for listener target '{target}'");
                return;
            }

            for (int i = 0; i < listeners.Length; i++)
            {
                var listener = listeners[i];
                if (!Utilities.IsValid(listener))
                {
                    Warn($"Invalid listener for target '{target}' at position {i}");
                    continue;
                }

                listener.SendCustomEvent(target);
            }
        }

        #endregion

        #region RPCs

        public void RPC_VersionUpdateAvailable()
        {
            if (!LocalPlayerSkipUpdateNotification)
            {
                Warn(
                    $"A player with a new build of the world joined. Please re-join the instance or " +
                    $"create a new instance to update if networking issues occur."
                );

                NotifyListeners(updateAvailableListeners, updateAvailableEvent);
            }

            HandleVersionConflict(true);
        }

        public void RPC_OutOfDatePlayerJoined()
        {
            Warn(
                $"A player with an old build of the world joined. It may be required to create a new " +
                $"instance if networking issues occur."
            );

            HandleVersionConflict(true);
        }

        #endregion
    }
}