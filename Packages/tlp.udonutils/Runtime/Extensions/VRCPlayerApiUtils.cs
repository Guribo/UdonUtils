using System.Text;
using JetBrains.Annotations;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Extensions
{
    public static class VrcPlayerApiUtils
    {
        [PublicAPI]
        public const int InvalidPlayerId = -1;

        [PublicAPI]
        public static int _PlayerIdSafe(this VRCPlayerApi playerApi) {
            return Utilities.IsValid(playerApi) ? playerApi.playerId : InvalidPlayerId;
        }

        [PublicAPI]
        public static string _DisplayNameSafe(this VRCPlayerApi playerApi) {
            return Utilities.IsValid(playerApi) ? playerApi.displayName : "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerApi"></param>
        /// <returns>returns 'Invalid Player' if player doesn't exist</returns>
        [PublicAPI]
        public static string _ToStringSafe(this VRCPlayerApi playerApi) {
            return Utilities.IsValid(playerApi)
                    ? $"Player {playerApi.playerId} ({nameof(playerApi.displayName)}={playerApi.displayName}, {nameof(playerApi.isLocal)}={playerApi.isLocal}, {nameof(playerApi.isMaster)}={playerApi.isMaster}, VR={playerApi.IsUserInVR()}, grounded={playerApi.IsPlayerGrounded()})"
                    : "Invalid Player";
        }

        [PublicAPI]
        public static VRCPlayerApi _IdToVrcPlayer(this int playerId) {
            return VRCPlayerApi.GetPlayerById(playerId);
        }

        public static bool _IsValidPlayer(this int playerId, out VRCPlayerApi player) {
            player = VRCPlayerApi.GetPlayerById(playerId);
            return Utilities.IsValid(player);
        }

        public static bool _IsRemoteSafe(this VRCPlayerApi player) {
            return Utilities.IsValid(player) && !player.isLocal;
        }

        public static bool _IsMasterSafe(this VRCPlayerApi player) {
            return Utilities.IsValid(player) && player.isMaster;
        }

        public static bool _IsLocalSafe(this VRCPlayerApi player) {
            return Utilities.IsValid(player) && player.isLocal;
        }

        public static bool _IsInstanceOwnerSafe(this VRCPlayerApi player) {
            return Utilities.IsValid(player) && player.isInstanceOwner;
        }

        public static bool _IsValidSafe(this VRCPlayerApi player) {
            return Utilities.IsValid(player);
        }

        public static VRCPlayerApi _GetMaster(this VRCPlayerApi[] players) {
            foreach (var player in players) {
                if (player._IsMasterSafe()) {
                    return player;
                }
            }

            return null;
        }

        /// <summary>
        /// Get all players in a new array
        /// </summary>
        /// <returns>freshly allocated array containing all players</returns>
        public static VRCPlayerApi[] _GetAllPlayers() {
            var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            return VRCPlayerApi.GetPlayers(players);
        }

        /// <param name="player">Must be valid</param>
        /// <returns>Name of the player followed by player ID, example: 'Foo (1)'</returns>
        public static string _DisplayNameUnique(this VRCPlayerApi player) {
            return new StringBuilder().Append(player.displayName).Append(" (").Append(player.playerId).Append(")")
                    .ToString();
        }

        /// <param name="player"></param>
        /// <returns>Name of the player followed by player ID, example: 'Foo (1)'; Empty string if player is invalid</returns>
        public static string _DisplayNameUniqueSafe(this VRCPlayerApi player) {
            if (Utilities.IsValid(player)) {
                return new StringBuilder().Append(player.displayName).Append(" (").Append(player.playerId).Append(")")
                        .ToString();
            }

            return "";
        }

        public static string _GetPlayerTagSafe(this VRCPlayerApi player, string tag) {
            if (Utilities.IsValid(player)) return player.GetPlayerTag(tag);
            return null;
        }

        public static bool _SetPlayerTagSafe(this VRCPlayerApi player, string tag, string value) {
            if (Utilities.IsValid(player)) {
                player.SetPlayerTag(tag, value);
                return true;
            }

            return false;
        }
    }
}