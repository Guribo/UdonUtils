using JetBrains.Annotations;
using TLP.UdonUtils.Events;
using TLP.UdonUtils.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using Time = UnityEngine.Time;

namespace TLP.UdonUtils.Sync
{
    /// <summary>
    /// Gives every player an equal chance to send something, ownership is controlled by the current master
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)] // TODO move me to the class declaration!
    public class RoundRobinSynchronizer : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.UiEnd + 1;


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
        public void ReturnOwnershipToMaster() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(ReturnOwnershipToMaster));
#endif
            #endregion


            if (_masterId.IsValidPlayer(out var master) && master.IsMasterSafe()) {
                Networking.SetOwner(master, gameObject);
                return;
            }

            master = GetMaster();
            _masterId = master.PlayerIdSafe();
            RequestSerialization();
            Networking.SetOwner(master, gameObject);
        }

        /// <summary>
        /// Use in OnDeserialization of scripts that rely on the RoundRobinSynchronizer
        /// </summary>
        public void ResetTimeout() {
            _expireTime = Time.timeSinceLevelLoad + SyncTimeoutSeconds;
            SendCustomEventDelayedSeconds(nameof(_CheckOwnershipReturned), SyncTimeoutSeconds);
        }
        #endregion

        #region Network Events
        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnOwnershipTransferred)} to {player.ToStringSafe()}");
#endif
            #endregion

            StartSyncOnClient();
        }

        public override void OnPlayerJoined(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnPlayerJoined)} {player.ToStringSafe()}");
#endif
            #endregion


            UpdatePlayerList();

            if (Networking.IsMaster) {
                SendCustomEventDelayedSeconds(nameof(_CheckOwnershipReturned), SyncTimeoutSeconds);
            }
        }


        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(
                    $"{nameof(OnOwnershipRequest)} from {requestingPlayer.ToStringSafe()} to set owner to {requestedOwner.ToStringSafe()}"
            );
#endif
            #endregion

            if (requestingPlayer.IsMasterSafe()) {
                // master is always allowed to request ownership
                return true;
            }

            if (!Utilities.IsValid(_currentPlayer) && requestedOwner.IsMasterSafe()) {
                // any player is allowed to return the ownership to the master if the target player is invalid
                return true;
            }

            // otherwise, only the target player is allowed to return ownership to the master
            return requestingPlayer.PlayerIdSafe() == _currentPlayer && requestedOwner.IsMasterSafe();
        }
        #endregion

        #region Internal
        private void UpdatePlayerList() {
            _players = _players.ResizeOrCreate(VRCPlayerApi.GetPlayerCount());
            VRCPlayerApi.GetPlayers(_players);
        }

        private VRCPlayerApi GetMaster() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(GetMaster));
#endif
            #endregion

            UpdatePlayerList();
            foreach (var vrcPlayerApi in _players) {
                if (vrcPlayerApi.IsMasterSafe()) {
                    return vrcPlayerApi;
                }
            }

            return null;
        }

        private void MasterTransferOwnershipToNextPlayer(VRCPlayerApi master) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(MasterTransferOwnershipToNextPlayer));
#endif
            #endregion

            if (_players.LengthSafe() == 0) {
                UpdatePlayerList();
            }

            int playerCount = VRCPlayerApi.GetPlayerCount();

            // for safety reasons we abort after each player has been visited, should not be needed as the loop
            // should generally finish after one iteration
            for (int i = 0; i < playerCount; i++) {
                _currentPlayerIndex.MoveIndexRightLooping(_players.Length);
                var nextPlayer = _players[_currentPlayerIndex];
                if (!Utilities.IsValid(nextPlayer)) {
                    UpdatePlayerList();
                    _currentPlayerIndex.MoveIndexRightLooping(_players.Length, 0);
                    nextPlayer = _players[_currentPlayerIndex];
                    if (!Utilities.IsValid(nextPlayer)) {
                        Error("Invalid player in player list even after updating the list!");
                        continue;
                    }
                }

                _masterId = master.playerId;
                _currentPlayer = nextPlayer.playerId;
                RequestSerialization();
                Networking.SetOwner(nextPlayer, gameObject);

                if (nextPlayer.isMaster) {
                    StartSyncOnClient();
                }

                break;
            }

            ResetTimeout();
        }


        public void _CheckOwnershipReturned() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(_CheckOwnershipReturned));
#endif
            #endregion

            if (Networking.IsMaster &&
                Time.timeSinceLevelLoad > _expireTime - Time.deltaTime) {
                var master = Networking.LocalPlayer;
                Networking.SetOwner(master, gameObject);
                MasterTransferOwnershipToNextPlayer(master);
            }
        }

        private void StartSyncOnClient() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(StartSyncOnClient));
#endif
            #endregion


            if (!Utilities.IsValid(StartSync)) {
                Error($"{nameof(StartSync)} not set");
                ReturnOwnershipToMaster();
                return;
            }

            if (!StartSync.Raise(this)) {
                Error($"Failed to raise {nameof(StartSync)}");
                ReturnOwnershipToMaster();
            }
        }
        #endregion
    }
}