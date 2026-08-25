using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sync.SyncedEvents;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Player
{
    [Serializable]
    public enum PlayerListResult
    {
        Success = 0,
        InvalidArgument = 1,
        AlreadyPresent = 2,
        NotPresent = 3
    }

    /// <summary>
    /// List of player IDs that can be sent to other players.
    /// If received, the local state is completely replaced by the remote state!
    /// Invalid IDs or duplicates are removed upon receiving.
    ///
    /// <remarks>DO NOT MODIFY <see cref="PlayerSet.WorkingValues"/> DIRECTLY!</remarks>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Any)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerSet), ExecutionOrder)]
    public class PlayerSet : SyncedEventIntArray
    {

        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = SyncedEventUrl.ExecutionOrder + 1;

        #region Constants
        private const string UnknownPlayerName = "UNKNOWN PLAYER";
        #endregion

        #region State
        /// <summary>
        /// Player Ids of added players for fast lookup
        /// </summary>
        private readonly DataDictionary _playerSet = new DataDictionary();

        /// <summary>
        /// Map of player IDs to player names of players that joined while the local player was present.
        /// </summary>
        private readonly DataDictionary _knownPlayerNames = new DataDictionary();
        #endregion

        #region Public API
        /// <param name="player"></param>
        /// <returns>Success, InvalidArgument if player invalid, AlreadyPresent</returns>
        public PlayerListResult _AddPlayer(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_AddPlayer)} {player._ToStringSafe()}");
#endif
            #endregion

            if (!Utilities.IsValid(player)) {
                _Error("Player invalid");
                return PlayerListResult.InvalidArgument;
            }

            if (_playerSet.ContainsKey(player.playerId)) {
                _Warn($"{player._DisplayNameSafe()} already present");
                return PlayerListResult.AlreadyPresent;
            }

            _DiscardInvalidPlayers();
            _playerSet.Add(player.playerId, new DataToken());
            _UpdateToNetworkData();
            return PlayerListResult.Success;
        }

        /// <param name="player"></param>
        /// <returns>Success, InvalidArgument if player invalid, NotPresent</returns>
        public PlayerListResult _RemovePlayer(VRCPlayerApi player) {
#if TLP_DEBUG
            _DebugLog($"{nameof(_RemovePlayer)} {player._ToStringSafe()}");
#endif
            if (!Utilities.IsValid(player)) {
                _Error("Player invalid");
                return PlayerListResult.InvalidArgument;
            }

            if (!_playerSet.Remove(player.playerId)) {
                _Warn($"{player._DisplayNameSafe()} was not in the list");
                return PlayerListResult.NotPresent;
            }

            _DiscardInvalidPlayers();
            _UpdateToNetworkData();
            return PlayerListResult.Success;
        }


        public bool _Contains(VRCPlayerApi playerApi) {
            bool result = _playerSet.ContainsKey(playerApi._PlayerIdSafe());

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_Contains)} {playerApi._ToStringSafe()}? {result}");
#endif
            #endregion

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>number of valid players in the list after disposing of all invalid ids</returns>
        public void _Clear() {
#if TLP_DEBUG
            _DebugLog(nameof(_Clear));
#endif
            _playerSet.Clear();
            WorkingValues = new int[0];
        }
        #endregion

        #region Overrides
        public override void OnPlayerJoined(VRCPlayerApi player) {
            base.OnPlayerJoined(player);

            if (!Utilities.IsValid(player) || _knownPlayerNames.ContainsKey(player.playerId)) {
                return;
            }

            _knownPlayerNames.Add(player.playerId, player.displayName);
        }

        public override void OnPreSerialization() {
            if (SyncPaused) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"{nameof(OnPreSerialization)} skipped (sync paused)");
#endif
                #endregion

                return;
            }

            base.OnPreSerialization();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            if (SyncPaused) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"{nameof(OnDeserialization)} skipped (sync paused)");
#endif
                #endregion

                return;
            }

            _UpdateFromNetworkData();
            base.OnDeserialization(deserializationResult);

            // ensure that any invalid entries or duplicates are removed
            _UpdateToNetworkData();
        }
        #endregion

        #region Internal
        private void _UpdateToNetworkData() {
            int playerCount = _playerSet.Count;
            WorkingValues = WorkingValues._ResizeOrCreate(playerCount);
            var players = _playerSet.GetKeys();
            for (int i = 0; i < playerCount; i++) {
                WorkingValues[i] = players[i].Int;
            }
        }

        internal void _DiscardInvalidPlayers() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_DiscardInvalidPlayers));
#endif
            #endregion

            var players = _playerSet.GetKeys();
            int playerCount = players.Count;

            for (int i = 0; i < playerCount; ++i) {
                int playerId = players[i].Int;
                if (Utilities.IsValid(playerId._IdToVrcPlayer())) {
                    continue;
                }

                _Warn($"Removing now invalid player id {playerId} ({_TryGetPlayerName(playerId)})");
                _playerSet.Remove(playerId);
            }
        }

        private void _UpdateFromNetworkData() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_UpdateFromNetworkData));
#endif
            #endregion

            _playerSet.Clear();
            foreach (int playerId in SyncedValues) {
                if (!playerId._IsValidPlayer(out var unused)) {
                    _Warn($"Removing now invalid player id {playerId} ({_TryGetPlayerName(playerId)})");
                    continue;
                }

                if (_playerSet.ContainsKey(playerId)) {
                    _Warn($"Removing duplicate player id {playerId} ({_TryGetPlayerName(playerId)})");
                    continue;
                }

                _playerSet.Add(playerId, new DataToken());
            }
        }

        private string _TryGetPlayerName(int playerId) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_TryGetPlayerName)} for player {playerId}");
#endif
            #endregion

            return _knownPlayerNames.ContainsKey(playerId) ? _knownPlayerNames[playerId].String : UnknownPlayerName;
        }
        #endregion
    }
}