using JetBrains.Annotations;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Extensions
{
    public static class VrcPlayerApiUtils
    {
        [PublicAPI]
        public static int PlayerIdSafe(this VRCPlayerApi playerApi)
        {
            if (Utilities.IsValid(playerApi))
            {
                return playerApi.playerId;
            }

            return -1;
        }

        [PublicAPI]
        public static string DisplayNameSafe(this VRCPlayerApi playerApi)
        {
            if (Utilities.IsValid(playerApi))
            {
                return playerApi.displayName;
            }

            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerApi"></param>
        /// <returns>returns 'Invalid Player' if player doesn't exist</returns>
        [PublicAPI]
        public static string ToStringSafe(this VRCPlayerApi playerApi)
        {
            if (Utilities.IsValid(playerApi))
            {
                return
                    $"Player {playerApi.playerId} ({nameof(playerApi.displayName)}={playerApi.displayName}, {nameof(playerApi.isLocal)}={playerApi.isLocal}, {nameof(playerApi.isMaster)}={playerApi.isMaster}, VR={playerApi.IsUserInVR()}, grounded={playerApi.IsPlayerGrounded()})";
            }

            return "Invalid Player";
        }

        [PublicAPI]
        public static VRCPlayerApi IdToVrcPlayer(this int playerId)
        {
            return VRCPlayerApi.GetPlayerById(playerId);
        }

        public static bool IsValidPlayer(this int playerId, out VRCPlayerApi player)
        {
            player = VRCPlayerApi.GetPlayerById(playerId);
            return Utilities.IsValid(player);
        }

        public static bool IsRemoteSafe(this VRCPlayerApi player)
        {
            return Utilities.IsValid(player) && !player.isLocal;
        }

        public static bool IsMasterSafe(this VRCPlayerApi player)
        {
            return Utilities.IsValid(player) && player.isMaster;
        }

        public static bool IsLocalSafe(this VRCPlayerApi player)
        {
            return Utilities.IsValid(player) && player.isLocal;
        }

        public static bool IsInstanceOwnerSafe(this VRCPlayerApi player)
        {
            return Utilities.IsValid(player) && player.isInstanceOwner;
        }
    }
}