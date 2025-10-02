using System.Globalization;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

#if UNITY_EDITOR
using TLP.UdonUtils.Runtime.Events;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonUtils.Editor.Events
{
    [CustomEditor(typeof(PlayerDataRestoredEvent))]
    public class PlayerDataRestoredEventEditor : UnityEditor.Editor
    {
        private const string Description =
                "Event system that tracks when players have their persistent data restored in VRChat worlds.\n\n" +
                "This component automatically detects when a player's data is loaded and maintains a registry of restored players using player tags. " +
                "It stores restoration timestamps and provides both event notifications and static methods to check player data status.\n\n" +
                "Key Features:\n" +
                "• Raises events when player data is restored\n" +
                "• Maintains a list of players with restored data\n" +
                "• Provides static IsPlayerDataRestored() method for queries\n" +
                "• Automatically cleans up when players leave\n" +
                "• Uses player tags for persistent tracking across the world\n\n" +
                "Use this to coordinate systems that depend on player data being available, such as user preferences, save data, or personalized content.";

        public override void OnInspectorGUI() {
            EditorGUILayout.HelpBox(Description, MessageType.Info);

            DrawDefaultInspector();
        }
    }
}
#endif

namespace TLP.UdonUtils.Runtime.Events
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerDataRestoredEvent), ExecutionOrder)]
    public class PlayerDataRestoredEvent : UdonEvent
    {
        #region ExecutionOrder
        [PublicAPI]
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.WorldInitStart + 10;
        #endregion

        private const string PlayerTagPrefix = "TLP/PlayerDataRestoredEvent/PlayerName/";

        [PublicAPI]
        public VRCPlayerApi RestoredPlayer { get; private set; }

        [PublicAPI]
        public readonly DataList RestoredPlayers = new DataList();

        public override void OnPlayerRestored(VRCPlayerApi player) {
            string uniquePlayerName = player.DisplayNameUniqueSafe();

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnPlayerRestored)}: {uniquePlayerName}");
#endif
            #endregion

            if (!Utilities.IsValid(player)) {
                Error($"{nameof(OnPlayerRestored)}: invalid player received");
                return;
            }

            if (!HasStartedOk) {
                return;
            }

            // store the information globally accessible in the player tags until the player leaves again
            LocalPlayer.SetPlayerTag(
                    PlayerTagPrefix + uniquePlayerName,
                    Time.timeSinceLevelLoadAsDouble.ToString(CultureInfo.InvariantCulture));
            if (!RestoredPlayers.Contains(uniquePlayerName)) {
                RestoredPlayers.Add(uniquePlayerName);
            }

            // notify all listeners with the restored player temporarily available
            RestoredPlayer = player;
            if (!Raise(this)) {
                Error($"{nameof(OnPlayerRestored)}: Failed to raise event");
            }

            RestoredPlayer = null;
        }

        public static bool IsPlayerDataRestored(VRCPlayerApi player) {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                TlpLogger.StaticError($"{nameof(IsPlayerDataRestored)}: {nameof(localPlayer)} invalid", null);
                return false;
            }

            string playerTag = localPlayer.GetPlayerTag($"{PlayerTagPrefix}{player.DisplayNameUnique()}");

            #region TLP_DEBUG
#if TLP_DEBUG
            TlpLogger.StaticDebugLog(
                    $"{nameof(IsPlayerDataRestored)}: {player.DisplayNameUniqueSafe()} = {!string.IsNullOrEmpty(playerTag)}",
                    null);
#endif
            #endregion

            return !string.IsNullOrEmpty(playerTag);
        }

        /// <summary>
        /// Removes a player from the local players tags and from this list
        /// </summary>
        /// <param name="player"></param>
        public override void OnPlayerLeft(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnPlayerLeft)}: {player.DisplayNameUniqueSafe()}");
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!Utilities.IsValid(player)) {
                Error($"{nameof(OnPlayerLeft)}: invalid player received");
                SendCustomEventDelayedFrames(nameof(Delayed_UpdateRestoredPlayers), 1);
                return;
            }

            string playerName = player.displayName;
            LocalPlayer.SetPlayerTag(PlayerTagPrefix + playerName);
            RestoredPlayers.RemoveAll(playerName);
        }

        #region Overrides
        public override void OnEnable() {
            base.OnEnable();
            Delayed_UpdateRestoredPlayers();
        }
        #endregion

        #region Delayed
        public void Delayed_UpdateRestoredPlayers() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Delayed_UpdateRestoredPlayers));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            var existingPlayers = CreateExistingPlayerLookupTable(out var existingPlayerNames);
            RemovePlayersWhoFailedLeaving(existingPlayerNames, LocalPlayer);
            AddRestoredButMissingPlayers(existingPlayers);
        }
        #endregion


        #region internal
        private void AddRestoredButMissingPlayers(VRCPlayerApi[] existingPlayers) {
            foreach (var existingPlayer in existingPlayers) {
                if (!Utilities.IsValid(existingPlayer)) {
                    continue;
                }

                string displayNameUnique = existingPlayer.DisplayNameUnique();
                if (!IsPlayerDataRestored(existingPlayer) || RestoredPlayers.Contains(displayNameUnique)) {
                    continue;
                }

                // player has already been restored but this gameObject doesn't know that yet: add to list
                RestoredPlayers.Add(displayNameUnique);
                Warn(
                        $"{nameof(AddRestoredButMissingPlayers)}: added {displayNameUnique} to {nameof(RestoredPlayers)}");
            }
        }

        private void RemovePlayersWhoFailedLeaving(
                DataDictionary existingPlayerNames,
                VRCPlayerApi localPlayer
        ) {
            for (int i = 0; i < RestoredPlayers.Count;) {
                var restoredPlayerName = RestoredPlayers[i];
                if (!existingPlayerNames.ContainsKey(restoredPlayerName)) {
                    // remove player, who left the world but for some reason still in the RestoredPlayers
                    localPlayer.SetPlayerTag(PlayerTagPrefix + restoredPlayerName);
                    RestoredPlayers.RemoveAll(restoredPlayerName);
                    Warn(
                            $"{nameof(RemovePlayersWhoFailedLeaving)}: removed {restoredPlayerName} from {nameof(RestoredPlayers)}");
                } else {
                    i++;
                }
            }
        }

        private static VRCPlayerApi[] CreateExistingPlayerLookupTable(out DataDictionary existingPlayerNames) {
            var existingPlayers = VrcPlayerApiUtils.GetAllPlayers();
            existingPlayerNames = new DataDictionary();
            foreach (var existingPlayer in existingPlayers) {
                if (Utilities.IsValid(existingPlayer) && !existingPlayerNames.ContainsKey(existingPlayer.displayName)) {
                    existingPlayerNames.Add(existingPlayer.displayName, 0);
                }
            }

            return existingPlayers;
        }
        #endregion
    }
}