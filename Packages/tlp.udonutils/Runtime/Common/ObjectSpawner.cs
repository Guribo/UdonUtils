using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Player;
using TLP.UdonUtils.Runtime.Sync;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ObjectSpawner), ExecutionOrder)]
    public class ObjectSpawner : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = RoundRobinSynchronizer.ExecutionOrder + 1;

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(Prefab)) {
                Error($"{nameof(Prefab)} not set");
                return false;
            }

            int playersCount = VRCPlayerApi.GetPlayerCount();
            var players = new VRCPlayerApi[playersCount];
            foreach (var vrcPlayerApi in VRCPlayerApi.GetPlayers(players)) {
                OnPlayerJoined(vrcPlayerApi);
            }

            return true;
        }

        [FormerlySerializedAs("prefab")] public GameObject Prefab;

        public override void OnPlayerJoined(VRCPlayerApi player) {
            if (!Utilities.IsValid(player)) {
                return;
            }

            var instance = Instantiate(Prefab);
            var trackingDataFollower = instance.GetComponent<TrackingDataFollower>();
            trackingDataFollower.UseLocalPlayerByDefault = false;
            trackingDataFollower.Player = player;
            instance.SetActive(true);
        }
    }
}