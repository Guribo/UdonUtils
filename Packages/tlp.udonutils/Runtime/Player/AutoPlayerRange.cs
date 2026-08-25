using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    /// <summary>
    /// Updates the far voice range of all players every time a player leaves or joins the world.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(AutoPlayerRange), ExecutionOrder)]
    public class AutoPlayerRange : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = CustomAudioFalloff.ExecutionOrder + 1;

        /// <summary>
        /// default voice far range defined by VRChat
        /// <remarks>https://docs.vrchat.com/docs/playewar-audio#set-voice-distance-far</remarks>
        /// </summary>
        internal const float DefaultRange = 25f;

        [FormerlySerializedAs("playerFarRangeMapping")]
        [Tooltip("Defines the far voice range of all players depending on the number of players in the world")]
        public AnimationCurve PlayerFarRangeMapping = AnimationCurve.Linear(1, 25, 80, 10);

        internal void OnEnable() {
            _UpdatePlayerVoiceRange(false);
        }

        internal void OnDisable() {
            _UpdatePlayerVoiceRange(true);
        }

        public override void OnPlayerJoined(VRCPlayerApi player) {
            _UpdatePlayerVoiceRange(false);
        }

        public override void OnPlayerLeft(VRCPlayerApi player) {
            _UpdatePlayerVoiceRange(false);
        }

        internal float _GetRange(AnimationCurve rangeMapping, int playerCount) {
            if (!Utilities.IsValid(rangeMapping)) {
                Debug.LogWarning("playerRangeMapping invalid, returning default range 25");
                return 25f;
            }

            return rangeMapping.Evaluate(playerCount);
        }

        internal void _UpdateVoiceRange(VRCPlayerApi[] playerApis, float voiceRange) {
            if (!Utilities.IsValid(playerApis)) {
                return;
            }

            foreach (var vrcPlayerApi in playerApis) {
                if (!Utilities.IsValid(vrcPlayerApi)) {
                    continue;
                }

                vrcPlayerApi.SetVoiceDistanceFar(voiceRange);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reset">if true all player voices are reset to use the <see cref="DefaultRange"/></param>
        internal void _UpdatePlayerVoiceRange(bool reset) {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            var players = new VRCPlayerApi[playerCount];
            VRCPlayerApi.GetPlayers(players);

            float range = reset ? DefaultRange : _GetRange(PlayerFarRangeMapping, playerCount);
            _UpdateVoiceRange(players, range);
        }
    }
}