using System.Globalization;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Logger;
using TLP.UdonUtils.Runtime.Player;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Events
{
    /// <summary>
    /// Notifies that a player had its persistent data loaded,
    /// stores this information in the local players playerTags
    /// using the <see cref="PlayerTagPrefix"/> + the players displayname as key,
    /// the value is the current time since level load (double as culture invariant string)
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerDataRestoredEvent), ExecutionOrder)]
    public class PlayerDataRestoredEvent : UdonEvent
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.WorldInitStart + 10;

        public const string PlayerTagPrefix = "TLP/PlayerDataRestoredEvent/PlayerName/";
        public VRCPlayerApi RestoredPlayer { get; private set; }
        public readonly DataList RestoredPlayers = new DataList();

        public override void OnPlayerRestored(VRCPlayerApi player) {
            if (!Utilities.IsValid(player)) {
                Error($"{nameof(OnPlayerRestored)}: invalid player received");
                return;
            }

            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                Error($"{nameof(OnPlayerRestored)}: {nameof(localPlayer)} invalid");
                return;
            }

            string uniquePlayerName = player.DisplayNameUnique();
            Info($"{nameof(OnPlayerRestored)}: {uniquePlayerName}");

            // store the information globally accessible in the player tags until the player leaves again
            localPlayer.SetPlayerTag(
                    PlayerTagPrefix + uniquePlayerName,
                    Time.timeSinceLevelLoadAsDouble.ToString(CultureInfo.InvariantCulture));
            if (!RestoredPlayers.Contains(uniquePlayerName)) {
                RestoredPlayers.Add(uniquePlayerName);
            }

            // notify all listeners with the restored player temporarily available
            RestoredPlayer = player;
            Raise(this);
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
            if (!Utilities.IsValid(player)) {
                Error($"{nameof(OnPlayerLeft)}: invalid player received");
                SendCustomEventDelayedFrames(nameof(UpdateRestoredPlayers), 1);
                return;
            }

            if (!Utilities.IsValid(Networking.LocalPlayer)) {
                return;
            }

            string playerName = player.displayName;
            Networking.LocalPlayer.SetPlayerTag(PlayerTagPrefix + playerName);
            RestoredPlayers.RemoveAll(playerName);
        }

        #region Overrides
        public override void OnEnable() {
            base.OnEnable();
            UpdateRestoredPlayers();
        }
        #endregion

        #region Delayed
        public void UpdateRestoredPlayers() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(UpdateRestoredPlayers));
#endif
            #endregion

            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                return;
            }

            var existingPlayers = CreateExistingPlayerLookupTable(out var existingPlayerNames);
            RemovePlayersWhoFailedLeaving(existingPlayerNames, localPlayer);
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