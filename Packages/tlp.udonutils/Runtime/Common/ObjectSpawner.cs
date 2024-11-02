using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Player;
using TLP.UdonUtils.Runtime.Sync;
using TLP.UdonUtils.Runtime.Sync.SyncedEvents;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ObjectSpawner), ExecutionOrder)]
    public class ObjectSpawner : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = RoundRobinSynchronizer.ExecutionOrder + 1;

        public override void Start() {
            base.Start();
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