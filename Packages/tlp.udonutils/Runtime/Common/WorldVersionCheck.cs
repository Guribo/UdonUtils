using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonUtils.Runtime.Common
{
    /// <summary>
    /// 1. Notifies other behaviours when a player with an old/new version of the current world
    ///    enters the current instance.
    /// 2. Notifies other behaviours and players when the local player enters the world and has
    ///    a new/old version of that world.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(WorldVersionCheck), ExecutionOrder)]
    public class WorldVersionCheck : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.WorldInitStart + 1;

        #region State

        [Header("Auto generated/updated on scene save")]


        [FormerlySerializedAs("timestamp")]
        public long Timestamp;
        [FormerlySerializedAs("build")]
        [Tooltip("Is automatically incremented during upload, can be set to an initial value")]
        public int Build;

        internal bool InstanceCompromised;
        internal bool LocalPlayerSkipUpdateNotification;
        internal int LocalVersionConflicts;
        #endregion

        #region Synced State
        [FormerlySerializedAs("syncedTotalVersionConflicts")]
        [UdonSynced]
        [HideInInspector]
        public int SyncedTotalVersionConflicts;

        [FormerlySerializedAs("syncedInstanceCompromised")]
        [UdonSynced]
        [HideInInspector]
        public bool SyncedInstanceCompromised;

        [FormerlySerializedAs("syncedBuild")]
        [UdonSynced]
        public int SyncedBuild;
        #endregion

        #region Events
        public const string UpdateAvailableEventName = "WorldUpdateAvailable";
        public const string VersionConflictEventName = "VersionConflictOccurred";

        [Header("Events")]
        [Tooltip("Event to raise when a new world update is available")]
        public UdonEvent UpdateAvailableEvent;

        [Tooltip("Event to raise when world version conflict between a joining player and the master occurs")]
        public UdonEvent VersionConflictOccurredEvent;
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(UpdateAvailableEvent)) {
                _ErrorAndDisableGameObject($"{nameof(UpdateAvailableEvent)} not set");
                return false;
            }

            if (UpdateAvailableEvent.ListenerMethod != UpdateAvailableEventName) {
                _Warn(
                        $"{nameof(UpdateAvailableEvent)}.{nameof(UdonEvent.ListenerMethod)} " +
                        $"needed to be changed to '{UpdateAvailableEventName}'");
                UpdateAvailableEvent.ListenerMethod = UpdateAvailableEventName;
            }


            if (!Utilities.IsValid(VersionConflictOccurredEvent)) {
                _ErrorAndDisableGameObject($"{nameof(VersionConflictOccurredEvent)} not set");
                return false;
            }

            if (VersionConflictOccurredEvent.ListenerMethod != VersionConflictEventName) {
                _Warn(
                        $"{nameof(VersionConflictOccurredEvent)}.{nameof(UdonEvent.ListenerMethod)} " +
                        $"needed to be changed to '{VersionConflictEventName}'");
                VersionConflictOccurredEvent.ListenerMethod = VersionConflictEventName;
            }

            _Info($"Build: {Build} Timestamp: {Timestamp}");

            if (Networking.IsMaster) {
                SyncedBuild = Build;
                RequestSerialization();
            } else {
                // ensure that other scripts have this frame start listening to the events.
                // Otherwise, we might fire the events without any listeners due to incorrect execution order.
                SendCustomEventDelayedFrames(nameof(_Delayed_CheckBuildRecency), 1);
            }

            return true;
        }

        #region Networking
        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnOwnershipTransferred)}: {player._ToStringSafe()}");
#endif
            #endregion

            if (!player._IsMasterSafe() || !player.isLocal) {
                return;
            }

            SyncedBuild = Build;
            RequestSerialization();
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(
                    $"{nameof(OnOwnershipTransferred)}: from {requestingPlayer._ToStringSafe()} " +
                    $"to {requestedOwner._ToStringSafe()}; " +
                    $"{(requestedOwner._IsMasterSafe() ? "Allowed" : "Denied")}");
#endif
            #endregion

            return requestedOwner._IsMasterSafe();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            bool versionConflictsAlreadyKnown = InstanceCompromised;
            if (versionConflictsAlreadyKnown) {
                _Warn(
                        $"There was {SyncedTotalVersionConflicts} version conflicts in this instance " +
                        $"({LocalVersionConflicts} conflicts were detected locally). " +
                        $"Consider creating a new instance."
                );
                return;
            }

            _Delayed_CheckBuildRecency();
        }
        #endregion
        #endregion

        #region RPCs
        [NetworkCallable(1)]
        public void _RPC_VersionUpdateAvailable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RPC_VersionUpdateAvailable));
#endif
            #endregion

            if (!LocalPlayerSkipUpdateNotification) {
                _Warn(
                        $"A player with a new build of the world joined. Please re-join the instance or " +
                        $"create a new instance to update if networking issues occur."
                );

                UpdateAvailableEvent._Raise(this);
            }

            _HandleVersionConflict(true);
        }

        [NetworkCallable(1)]
        public void _RPC_OutOfDatePlayerJoined() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RPC_OutOfDatePlayerJoined));
#endif
            #endregion

            _Warn(
                    $"A player with an old build of the world joined. It may be required to create a new " +
                    $"instance if networking issues occur."
            );

            _HandleVersionConflict(true);
        }
        #endregion

        #region Internal
        public void _Delayed_CheckBuildRecency() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Delayed_CheckBuildRecency));
#endif
            #endregion

            bool localPlayerNeedsToUpdate = SyncedBuild > Build;
            if (localPlayerNeedsToUpdate) {
                _LocalVersionOutOfDate();
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(_RPC_OutOfDatePlayerJoined));
                return;
            }

            bool worldUpdateAvailable = Build > SyncedBuild;
            if (worldUpdateAvailable) {
                LocalPlayerSkipUpdateNotification = true;
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(_RPC_VersionUpdateAvailable));
                return;
            }

            if (SyncedInstanceCompromised) {
                _HandleVersionConflict(false);
            }
        }

        internal void _LocalVersionOutOfDate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_LocalVersionOutOfDate));
#endif
            #endregion

            _Warn(
                    $"The world is {SyncedBuild - Build} builds out of date! Please clear your VRChat cache " +
                    $"and restart the game."
            );
            UpdateAvailableEvent._Raise(this);
        }

        internal void _HandleVersionConflict(bool locallyDetected) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_HandleVersionConflict));
#endif
            #endregion

            InstanceCompromised = true;

            if (locallyDetected) {
                LocalVersionConflicts++;
            }

            if (Networking.IsMaster) {
                if (locallyDetected) {
                    ++SyncedTotalVersionConflicts;
                }

                SyncedInstanceCompromised = true;
                RequestSerialization();
            }

            _Warn(
                    $"There was {SyncedTotalVersionConflicts} version conflicts in this instance " +
                    $"({LocalVersionConflicts} conflicts were detected locally). " +
                    $"Consider creating a new instance."
            );

            VersionConflictOccurredEvent._Raise(this);
        }
        #endregion
    }
}