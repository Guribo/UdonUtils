using System;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sync.SyncedEvents;
using UdonSharp;
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
    public class PlayerSet : SyncedEventIntArray
    {
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
        public PlayerListResult AddPlayer(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(AddPlayer)} {player.ToStringSafe()}");
#endif
            #endregion

            if (!Utilities.IsValid(player)) {
                Error("Player invalid");
                return PlayerListResult.InvalidArgument;
            }

            if (_playerSet.ContainsKey(player.playerId)) {
                Warn($"{player.DisplayNameSafe()} already present");
                return PlayerListResult.AlreadyPresent;
            }

            DiscardInvalidPlayers();
            _playerSet.Add(player.playerId, new DataToken());
            UpdateToNetworkData();
            return PlayerListResult.Success;
        }

        /// <param name="player"></param>
        /// <returns>Success, InvalidArgument if player invalid, NotPresent</returns>
        public PlayerListResult RemovePlayer(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog($"{nameof(RemovePlayer)} {player.ToStringSafe()}");
#endif
            if (!Utilities.IsValid(player)) {
                Error("Player invalid");
                return PlayerListResult.InvalidArgument;
            }

            if (!_playerSet.Remove(player.playerId)) {
                Warn($"{player.DisplayNameSafe()} was not in the list");
                return PlayerListResult.NotPresent;
            }

            DiscardInvalidPlayers();
            UpdateToNetworkData();
            return PlayerListResult.Success;
        }


        public bool Contains(VRCPlayerApi playerApi) {
            bool result = _playerSet.ContainsKey(playerApi.PlayerIdSafe());

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(Contains)} {playerApi.ToStringSafe()}? {result}");
#endif
            #endregion

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>number of valid players in the list after disposing of all invalid ids</returns>
        public void Clear() {
#if TLP_DEBUG
            DebugLog(nameof(Clear));
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
                DebugLog($"{nameof(OnPreSerialization)} skipped (sync paused)");
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
                DebugLog($"{nameof(OnDeserialization)} skipped (sync paused)");
#endif
                #endregion

                return;
            }

            UpdateFromNetworkData();
            base.OnDeserialization(deserializationResult);

            // ensure that any invalid entries or duplicates are removed
            UpdateToNetworkData();
        }
        #endregion

        #region Internal
        private void UpdateToNetworkData() {
            int playerCount = _playerSet.Count;
            WorkingValues = WorkingValues.ResizeOrCreate(playerCount);
            var players = _playerSet.GetKeys();
            for (int i = 0; i < playerCount; i++) {
                WorkingValues[i] = players[i].Int;
            }
        }

        internal void DiscardInvalidPlayers() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(DiscardInvalidPlayers));
#endif
            #endregion

            var players = _playerSet.GetKeys();
            int playerCount = players.Count;

            for (int i = 0; i < playerCount; ++i) {
                int playerId = players[i].Int;
                if (Utilities.IsValid(playerId.IdToVrcPlayer())) {
                    continue;
                }

                Warn($"Removing now invalid player id {playerId} ({TryGetPlayerName(playerId)})");
                _playerSet.Remove(playerId);
            }
        }

        private void UpdateFromNetworkData() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(UpdateFromNetworkData));
#endif
            #endregion

            _playerSet.Clear();
            foreach (int playerId in SyncedValues) {
                if (!playerId.IsValidPlayer(out var unused)) {
                    Warn($"Removing now invalid player id {playerId} ({TryGetPlayerName(playerId)})");
                    continue;
                }

                if (_playerSet.ContainsKey(playerId)) {
                    Warn($"Removing duplicate player id {playerId} ({TryGetPlayerName(playerId)})");
                    continue;
                }

                _playerSet.Add(playerId, new DataToken());
            }
        }

        private string TryGetPlayerName(int playerId) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(TryGetPlayerName)} for player {playerId}");
#endif
            #endregion

            return _knownPlayerNames.ContainsKey(playerId) ? _knownPlayerNames[playerId].String : UnknownPlayerName;
        }
        #endregion
    }
}