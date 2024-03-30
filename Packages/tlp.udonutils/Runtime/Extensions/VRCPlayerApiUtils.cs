using JetBrains.Annotations;
using VRC.SDKBase;

namespace TLP.UdonUtils.Extensions
{
    public static class VrcPlayerApiUtils
    {
        [PublicAPI]
        public const int InvalidPlayerId = -1;

        [PublicAPI]
        public static int PlayerIdSafe(this VRCPlayerApi playerApi) {
            return Utilities.IsValid(playerApi) ? playerApi.playerId : InvalidPlayerId;
        }

        [PublicAPI]
        public static string DisplayNameSafe(this VRCPlayerApi playerApi) {
            return Utilities.IsValid(playerApi) ? playerApi.displayName : "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerApi"></param>
        /// <returns>returns 'Invalid Player' if player doesn't exist</returns>
        [PublicAPI]
        public static string ToStringSafe(this VRCPlayerApi playerApi) {
            return Utilities.IsValid(playerApi)
                    ? $"Player {playerApi.playerId} ({nameof(playerApi.displayName)}={playerApi.displayName}, {nameof(playerApi.isLocal)}={playerApi.isLocal}, {nameof(playerApi.isMaster)}={playerApi.isMaster}, VR={playerApi.IsUserInVR()}, grounded={playerApi.IsPlayerGrounded()})"
                    : "Invalid Player";
        }

        [PublicAPI]
        public static VRCPlayerApi IdToVrcPlayer(this int playerId) {
            return VRCPlayerApi.GetPlayerById(playerId);
        }

        public static bool IsValidPlayer(this int playerId, out VRCPlayerApi player) {
            player = VRCPlayerApi.GetPlayerById(playerId);
            return Utilities.IsValid(player);
        }

        public static bool IsRemoteSafe(this VRCPlayerApi player) {
            return Utilities.IsValid(player) && !player.isLocal;
        }

        public static bool IsMasterSafe(this VRCPlayerApi player) {
            return Utilities.IsValid(player) && player.isMaster;
        }

        public static bool IsLocalSafe(this VRCPlayerApi player) {
            return Utilities.IsValid(player) && player.isLocal;
        }

        public static bool IsInstanceOwnerSafe(this VRCPlayerApi player) {
            return Utilities.IsValid(player) && player.isInstanceOwner;
        }

        public static bool IsValidSafe(this VRCPlayerApi player) {
            return Utilities.IsValid(player);
        }

        public static VRCPlayerApi GetMaster(this VRCPlayerApi[] players) {
            foreach (var player in players) {
                if (player.IsMasterSafe()) {
                    return player;
                }
            }

            return null;
        }
    }
}