using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sync.SyncedEvents;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using Time = UnityEngine.Time;

namespace TLP.UdonUtils.Runtime.Sync
{
    /// <summary>
    /// Gives every player an equal chance to send something, ownership is controlled by the current master
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(RoundRobinSynchronizer), ExecutionOrder)]
    public class RoundRobinSynchronizer : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = SyncedEvent.ExecutionOrder + 100;


        private VRCPlayerApi[] _players;

        [UdonSynced]
        private int _currentPlayer;

        [UdonSynced]
        private int _masterId;

        private int _currentPlayerIndex;

        private float _expireTime;

        public UdonEvent StartSync;

        [SerializeField]
        [Tooltip(
                "If the ownership hasn't been returned to the master after this amount of seconds the master will move to the next player"
        )]
        private int SyncTimeoutSeconds = 3;

        #region Public API
        public void _ReturnOwnershipToMaster() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_ReturnOwnershipToMaster));
#endif
            #endregion

            if (_masterId._IsValidPlayer(out var master) && master._IsMasterSafe()) {
                Networking.SetOwner(master, gameObject);
                return;
            }

            master = _GetMaster();
            _masterId = master._PlayerIdSafe();
            RequestSerialization();
            Networking.SetOwner(master, gameObject);
        }

        /// <summary>
        /// Use in OnDeserialization of scripts that rely on the RoundRobinSynchronizer
        /// </summary>
        public void _ResetTimeout() {
            _expireTime = Time.timeSinceLevelLoad + SyncTimeoutSeconds;
            SendCustomEventDelayedSeconds(nameof(_CheckOwnershipReturned), SyncTimeoutSeconds);
        }
        #endregion

        #region Network Events
        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnOwnershipTransferred)} to {player._ToStringSafe()}");
#endif
            #endregion

            _StartSyncOnClient();
        }

        public override void OnPlayerJoined(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnPlayerJoined)} {player._ToStringSafe()}");
#endif
            #endregion


            _UpdatePlayerList();

            if (Networking.IsMaster) {
                SendCustomEventDelayedSeconds(nameof(_CheckOwnershipReturned), SyncTimeoutSeconds);
            }
        }


        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(
                    $"{nameof(OnOwnershipRequest)} from {requestingPlayer._ToStringSafe()} to set owner to {requestedOwner._ToStringSafe()}"
            );
#endif
            #endregion

            if (requestingPlayer._IsMasterSafe()) {
                // master is always allowed to request ownership
                return true;
            }

            if (!Utilities.IsValid(_currentPlayer) && requestedOwner._IsMasterSafe()) {
                // any player is allowed to return the ownership to the master if the target player is invalid
                return true;
            }

            // otherwise, only the target player is allowed to return ownership to the master
            return requestingPlayer._PlayerIdSafe() == _currentPlayer && requestedOwner._IsMasterSafe();
        }
        #endregion

        #region Internal
        private void _UpdatePlayerList() {
            _players = _players._ResizeOrCreate(VRCPlayerApi.GetPlayerCount());
            VRCPlayerApi.GetPlayers(_players);
        }

        private VRCPlayerApi _GetMaster() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_GetMaster));
#endif
            #endregion

            _UpdatePlayerList();
            foreach (var vrcPlayerApi in _players) {
                if (vrcPlayerApi._IsMasterSafe()) {
                    return vrcPlayerApi;
                }
            }

            return null;
        }

        private void _MasterTransferOwnershipToNextPlayer(VRCPlayerApi master) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_MasterTransferOwnershipToNextPlayer));
#endif
            #endregion

            if (_players._LengthSafe() == 0) {
                _UpdatePlayerList();
            }

            int playerCount = VRCPlayerApi.GetPlayerCount();

            // for safety reasons we abort after each player has been visited, should not be needed as the loop
            // should generally finish after one iteration
            for (int i = 0; i < playerCount; i++) {
                _currentPlayerIndex._MoveIndexRightLooping(_players.Length);
                var nextPlayer = _players[_currentPlayerIndex];
                if (!Utilities.IsValid(nextPlayer)) {
                    _UpdatePlayerList();
                    _currentPlayerIndex._MoveIndexRightLooping(_players.Length, 0);
                    nextPlayer = _players[_currentPlayerIndex];
                    if (!Utilities.IsValid(nextPlayer)) {
                        _Error("Invalid player in player list even after updating the list!");
                        continue;
                    }
                }

                _masterId = master.playerId;
                _currentPlayer = nextPlayer.playerId;
                RequestSerialization();
                Networking.SetOwner(nextPlayer, gameObject);

                if (nextPlayer.isMaster) {
                    _StartSyncOnClient();
                }

                break;
            }

            _ResetTimeout();
        }


        public void _CheckOwnershipReturned() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_CheckOwnershipReturned));
#endif
            #endregion

            if (Networking.IsMaster &&
                Time.timeSinceLevelLoad > _expireTime - Time.deltaTime) {
                var master = Networking.LocalPlayer;
                Networking.SetOwner(master, gameObject);
                _MasterTransferOwnershipToNextPlayer(master);
            }
        }

        private void _StartSyncOnClient() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_StartSyncOnClient));
#endif
            #endregion


            if (!Utilities.IsValid(StartSync)) {
                _Error($"{nameof(StartSync)} not set");
                _ReturnOwnershipToMaster();
                return;
            }

            if (!StartSync._Raise(this)) {
                _Error($"Failed to raise {nameof(StartSync)}");
                _ReturnOwnershipToMaster();
            }
        }
        #endregion
    }
}