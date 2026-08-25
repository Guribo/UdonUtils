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

        protected override bool _SetupAndValidate() {
            if (Networking.IsOwner(gameObject)) {
                SendCustomEventDelayedSeconds(nameof(_TrackStats), 3f);
                _grounded = Networking.LocalPlayer.IsPlayerGrounded();
                _playerPosition = Networking.LocalPlayer.GetPosition();
            }

            return base._SetupAndValidate();
        }

        public void _TrackStats() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TrackStats));
#endif
            #endregion

            if (!Utilities.IsValid(Networking.LocalPlayer)) {
                return;
            }

            bool grounded = Networking.LocalPlayer.IsPlayerGrounded();
            var playerPosition = Networking.LocalPlayer.GetPosition();


            if (grounded && _grounded) {
                if (_playersWorking._LengthSafe() < 1) {
                    _playersWorking = new string[1];
                }

                if (_distanceWorking._LengthSafe() != _playersWorking._LengthSafe()) {
                    _distanceWorking = _distanceWorking._ResizeOrCreate(_playersWorking._LengthSafe());
                }

                _playersWorking[0] = Networking.LocalPlayer.displayName;
                _distanceWorking[0] += Vector3.Distance(playerPosition, _playerPosition);
            }

            _grounded = grounded;
            _playerPosition = playerPosition;

            if (Mathf.Abs(_lastDistance - _distanceWorking[0]) > 1f) {
                _lastDistance = _distanceWorking[0];

                _Info(
                        $"{nameof(_TrackStats)}: {Networking.LocalPlayer._DisplayNameSafe()} " +
                        $"has traveled {_distanceWorking[0]} meters on the ground");
                _MarkNetworkDirty();
            }

            SendCustomEventDelayedSeconds(nameof(_TrackStats), 5f);
        }

        public override void OnPreSerialization() {
            base.OnPreSerialization();

            if (_playersWorking._LengthSafe() == 0) {
                _playersWorking = new string[0];
            }

            if (_distanceWorking._LengthSafe() != _playersWorking._LengthSafe()) {
                _distanceWorking = _distanceWorking._ResizeOrCreate(_playersWorking._LengthSafe());
            }

            _playersWorking._CreateCopy(ref Players);
            _distanceWorking._CreateCopy(ref Distance);
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);


            Players._CreateCopy(ref _playersWorking);
            Distance._CreateCopy(ref _distanceWorking);

            if (_playersWorking._LengthSafe() == 0) {
                _playersWorking = new string[0];
            }

            if (_distanceWorking._LengthSafe() != _playersWorking._LengthSafe()) {
                _distanceWorking = _distanceWorking._ResizeOrCreate(_playersWorking._LengthSafe());
            }

            if (!Networking.IsOwner(gameObject) || _playersWorking._LengthSafe() < 1) {
                return;
            }

            // merge received state with local player state
            if (Utilities.IsValid(_localPlayerInstance)) {
                _localPlayerInstance._UpdatePlayer(_playersWorking[0], _distanceWorking[0]);
            } else {
                // find local player object
                var localPlayersObjects = Networking.GetPlayerObjects(Networking.LocalPlayer);
                foreach (var localPlayerObject in localPlayersObjects) {
                    _localPlayerInstance = localPlayerObject.GetComponent<PrototypeGlobalStats>();
                    if (Utilities.IsValid(_localPlayerInstance)) break;
                }

                if (!Utilities.IsValid(_localPlayerInstance)) {
                    _Error($"Local player has no {nameof(PrototypeGlobalStats)} yet");
                    return;
                }

                _Info(
                        $"Merging stats from {Networking.GetOwner(gameObject).displayName}" +
                        $"with local player {Networking.LocalPlayer.displayName}");
                _localPlayerInstance._Merge(_playersWorking, _distanceWorking);
            }
        }

        private void _Merge(string[] players, float[] distances) {
            for (int i = 0; i < players._LengthSafe() && i < distances._LengthSafe(); i++) {
                _UpdatePlayer(players[i], distances[i]);
            }
        }

        private void _UpdatePlayer(string playerName, float distance) {
            _Info($"{nameof(_UpdatePlayer)}: {playerName} has traveled {distance} meters on the ground");


            if (_playersWorking._LengthSafe() != _distanceWorking._LengthSafe()) {
                _distanceWorking = _distanceWorking._ResizeOrCreate(_playersWorking._LengthSafe());
            }

            for (int i = 0; i < _playersWorking._LengthSafe(); i++) {
                if (_playersWorking[i] != playerName) {
                    continue;
                }

                _distanceWorking[i] = Mathf.Max(distance, _distanceWorking[i]);
                return;
            }

            _playersWorking = _playersWorking._ResizeOrCreate(_playersWorking._LengthSafe() + 1);
            _distanceWorking = _distanceWorking._ResizeOrCreate(_playersWorking._LengthSafe());
            _playersWorking[_playersWorking._LengthSafe() - 1] = playerName;
            _distanceWorking[_distanceWorking._LengthSafe() - 1] = distance;
        }
    }
}