using TLP.UdonUtils.Runtime.Player;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectSpawner : UdonSharpBehaviour
    {
        public void Start() {
            int playersCount = VRCPlayerApi.GetPlayerCount();
            var players = new VRCPlayerApi[playersCount];
            foreach (var vrcPlayerApi in VRCPlayerApi.GetPlayers(players)) {
                OnPlayerJoined(vrcPlayerApi);
            }
        }

        public GameObject prefab;

        public override void OnPlayerJoined(VRCPlayerApi player) {
            if (!Utilities.IsValid(player)) {
                return;
            }

            var instance = Instantiate(prefab);
            var trackingDataFollower = instance.GetComponent<TrackingDataFollower>();
            trackingDataFollower.UseLocalPlayerByDefault = false;
            trackingDataFollower.Player = player;
            instance.SetActive(true);
        }
    }
}