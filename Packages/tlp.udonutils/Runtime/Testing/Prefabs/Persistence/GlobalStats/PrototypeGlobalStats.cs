using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Testing.Persistence
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PrototypeGlobalStats), ExecutionOrder)]
    public class PrototypeGlobalStats : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.RecordingStart + 50;

        [UdonSynced]
        public string[] Players;

        [UdonSynced]
        public float[] Distance;

        private string[] _playersWorking;
        private float[] _distanceWorking;
        private float _lastDistance;

        private PrototypeGlobalStats _localPlayerInstance;
        private Vector3 _playerPosition;
        private bool _grounded;

        protected override bool SetupAndValidate() {
            if (Networking.IsOwner(gameObject)) {
                SendCustomEventDelayedSeconds(nameof(TrackStats), 3f);
                _grounded = Networking.LocalPlayer.IsPlayerGrounded();
                _playerPosition = Networking.LocalPlayer.GetPosition();
            }

            return base.SetupAndValidate();
        }

        public void TrackStats() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TrackStats));
#endif
            #endregion

            if (!Utilities.IsValid(Networking.LocalPlayer)) {
                return;
            }

            bool grounded = Networking.LocalPlayer.IsPlayerGrounded();
            var playerPosition = Networking.LocalPlayer.GetPosition();


            if (grounded && _grounded) {
                if (_playersWorking.LengthSafe() < 1) {
                    _playersWorking = new string[1];
                }

                if (_distanceWorking.LengthSafe() != _playersWorking.LengthSafe()) {
                    _distanceWorking = _distanceWorking.ResizeOrCreate(_playersWorking.LengthSafe());
                }

                _playersWorking[0] = Networking.LocalPlayer.displayName;
                _distanceWorking[0] += Vector3.Distance(playerPosition, _playerPosition);
            }

            _grounded = grounded;
            _playerPosition = playerPosition;

            if (Mathf.Abs(_lastDistance - _distanceWorking[0]) > 1f) {
                _lastDistance = _distanceWorking[0];

                Info(
                        $"{nameof(TrackStats)}: {Networking.LocalPlayer.DisplayNameSafe()} " +
                        $"has traveled {_distanceWorking[0]} meters on the ground");
                MarkNetworkDirty();
            }

            SendCustomEventDelayedSeconds(nameof(TrackStats), 5f);
        }

        public override void OnPreSerialization() {
            base.OnPreSerialization();

            if (_playersWorking.LengthSafe() == 0) {
                _playersWorking = new string[0];
            }

            if (_distanceWorking.LengthSafe() != _playersWorking.LengthSafe()) {
                _distanceWorking = _distanceWorking.ResizeOrCreate(_playersWorking.LengthSafe());
            }

            _playersWorking.CreateCopy(ref Players);
            _distanceWorking.CreateCopy(ref Distance);
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);


            Players.CreateCopy(ref _playersWorking);
            Distance.CreateCopy(ref _distanceWorking);

            if (_playersWorking.LengthSafe() == 0) {
                _playersWorking = new string[0];
            }

            if (_distanceWorking.LengthSafe() != _playersWorking.LengthSafe()) {
                _distanceWorking = _distanceWorking.ResizeOrCreate(_playersWorking.LengthSafe());
            }

            if (!Networking.IsOwner(gameObject) || _playersWorking.LengthSafe() < 1) {
                return;
            }

            // merge received state with local player state
            if (Utilities.IsValid(_localPlayerInstance)) {
                _localPlayerInstance.UpdatePlayer(_playersWorking[0], _distanceWorking[0]);
            } else {
                // find local player object
                var localPlayersObjects = Networking.GetPlayerObjects(Networking.LocalPlayer);
                foreach (var localPlayerObject in localPlayersObjects) {
                    _localPlayerInstance = localPlayerObject.GetComponent<PrototypeGlobalStats>();
                    if (Utilities.IsValid(_localPlayerInstance)) break;
                }

                if (!Utilities.IsValid(_localPlayerInstance)) {
                    Error($"Local player has no {nameof(PrototypeGlobalStats)} yet");
                    return;
                }

                Info(
                        $"Merging stats from {Networking.GetOwner(gameObject).displayName}" +
                        $"with local player {Networking.LocalPlayer.displayName}");
                _localPlayerInstance.Merge(_playersWorking, _distanceWorking);
            }
        }

        private void Merge(string[] players, float[] distances) {
            for (int i = 0; i < players.LengthSafe() && i < distances.LengthSafe(); i++) {
                UpdatePlayer(players[i], distances[i]);
            }
        }

        private void UpdatePlayer(string playerName, float distance) {
            Info($"{nameof(UpdatePlayer)}: {playerName} has traveled {distance} meters on the ground");


            if (_playersWorking.LengthSafe() != _distanceWorking.LengthSafe()) {
                _distanceWorking = _distanceWorking.ResizeOrCreate(_playersWorking.LengthSafe());
            }

            for (int i = 0; i < _playersWorking.LengthSafe(); i++) {
                if (_playersWorking[i] != playerName) {
                    continue;
                }

                _distanceWorking[i] = Mathf.Max(distance, _distanceWorking[i]);
                return;
            }

            _playersWorking = _playersWorking.ResizeOrCreate(_playersWorking.LengthSafe() + 1);
            _distanceWorking = _distanceWorking.ResizeOrCreate(_playersWorking.LengthSafe());
            _playersWorking[_playersWorking.LengthSafe() - 1] = playerName;
            _distanceWorking[_distanceWorking.LengthSafe() - 1] = distance;
        }
    }
}