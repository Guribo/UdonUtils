using System.Globalization;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;
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
            string uniquePlayerName = player._DisplayNameUniqueSafe();

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnPlayerRestored)}: {uniquePlayerName}");
#endif
            #endregion

            if (!Utilities.IsValid(player)) {
                _Error($"{nameof(OnPlayerRestored)}: invalid player received");
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
            if (!_Raise(this)) {
                _Error($"{nameof(OnPlayerRestored)}: Failed to raise event");
            }

            RestoredPlayer = null;
        }

        public static bool _IsPlayerDataRestored(VRCPlayerApi player) {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                TlpLogger.StaticError($"{nameof(_IsPlayerDataRestored)}: {nameof(localPlayer)} invalid", null);
                return false;
            }

            string playerTag = localPlayer.GetPlayerTag($"{PlayerTagPrefix}{player._DisplayNameUnique()}");

            #region TLP_DEBUG
#if TLP_DEBUG
            TlpLogger.StaticDebugLog(
                    $"{nameof(_IsPlayerDataRestored)}: {player._DisplayNameUniqueSafe()} = {!string.IsNullOrEmpty(playerTag)}",
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
            _DebugLog($"{nameof(OnPlayerLeft)}: {player._DisplayNameUniqueSafe()}");
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            if (!Utilities.IsValid(player)) {
                _Error($"{nameof(OnPlayerLeft)}: invalid player received");
                SendCustomEventDelayedFrames(nameof(_Delayed_UpdateRestoredPlayers), 1);
                return;
            }

            string playerName = player.displayName;
            LocalPlayer.SetPlayerTag(PlayerTagPrefix + playerName);
            RestoredPlayers.RemoveAll(playerName);
        }

        #region Overrides
        protected override void OnEnable() {
            base.OnEnable();

            _Delayed_UpdateRestoredPlayers();
        }
        #endregion

        #region Delayed
        public void _Delayed_UpdateRestoredPlayers() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Delayed_UpdateRestoredPlayers));
#endif
            #endregion

            if (!HasStartedOk) {
                return;
            }

            var existingPlayers = _CreateExistingPlayerLookupTable(out var existingPlayerNames);
            _RemovePlayersWhoFailedLeaving(existingPlayerNames, LocalPlayer);
            _AddRestoredButMissingPlayers(existingPlayers);
        }
        #endregion


        #region internal
        private void _AddRestoredButMissingPlayers(VRCPlayerApi[] existingPlayers) {
            foreach (var existingPlayer in existingPlayers) {
                if (!Utilities.IsValid(existingPlayer)) {
                    continue;
                }

                string displayNameUnique = existingPlayer._DisplayNameUnique();
                if (!_IsPlayerDataRestored(existingPlayer) || RestoredPlayers.Contains(displayNameUnique)) {
                    continue;
                }

                // player has already been restored but this gameObject doesn't know that yet: add to list
                RestoredPlayers.Add(displayNameUnique);
                _Warn(
                        $"{nameof(_AddRestoredButMissingPlayers)}: added {displayNameUnique} to {nameof(RestoredPlayers)}");
            }
        }

        private void _RemovePlayersWhoFailedLeaving(
                DataDictionary existingPlayerNames,
                VRCPlayerApi localPlayer
        ) {
            for (int i = 0; i < RestoredPlayers.Count;) {
                var restoredPlayerName = RestoredPlayers[i];
                if (!existingPlayerNames.ContainsKey(restoredPlayerName)) {
                    // remove player, who left the world but for some reason still in the RestoredPlayers
                    localPlayer.SetPlayerTag(PlayerTagPrefix + restoredPlayerName);
                    RestoredPlayers.RemoveAll(restoredPlayerName);
                    _Warn(
                            $"{nameof(_RemovePlayersWhoFailedLeaving)}: removed {restoredPlayerName} from {nameof(RestoredPlayers)}");
                } else {
                    i++;
                }
            }
        }

        private static VRCPlayerApi[] _CreateExistingPlayerLookupTable(out DataDictionary existingPlayerNames) {
            var existingPlayers = VrcPlayerApiUtils._GetAllPlayers();
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