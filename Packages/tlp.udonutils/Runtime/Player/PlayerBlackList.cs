using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sync;
using TLP.UdonUtils.Runtime.Sync.SyncedEvents;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerBlackList), ExecutionOrder)]
    public class PlayerBlackList : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = SyncedPlayerSet.ExecutionOrder + 1;

        [SerializeField]
        internal SyncedEventStringArray BlackListedPlayers;

        [SerializeField]
        internal SyncedEventStringArray WhiteListedPlayers;

        [Tooltip(
                "Optional URL to text that contains all players that shall be blacklisted. Entries must be separated by linebreak (\\n or \\r\\n). Refreshed every time this behaviour is enabled."
        )]
        [SerializeField]
        private VRCUrl OptionalBlackListUrl;

        [Tooltip(
                "Optional URL to text that contains all players that shall be whitelisted. Entries must be separated by linebreak (\\n or \\r\\n). Refreshed every time this behaviour is enabled."
        )]
        [SerializeField]
        private VRCUrl OptionalWhiteListUrl;

        [Tooltip(
                "Optional text file that contains all players that shall be blacklisted. Entries must be separated by linebreak (\\n or \\r\\n)."
        )]
        [SerializeField]
        internal TextAsset OptionalInitialBlackListedPlayerNames;

        [Tooltip(
                "Optional text file that contains all players that shall be whitelisted. Entries must be separated by linebreak (\\n or \\r\\n)."
        )]
        [SerializeField]
        internal TextAsset OptionalInitialWhiteListedPlayerNames;

        private readonly DataDictionary _initialBlackListedEntries = new DataDictionary();
        private readonly DataDictionary _initialWhiteListedEntries = new DataDictionary();
        private readonly DataDictionary _dynamicallyWhiteListedEntries = new DataDictionary();
        private readonly DataDictionary _dynamicallyBlackListedEntries = new DataDictionary();
        private readonly DataDictionary _blackListed = new DataDictionary();
        private readonly DataDictionary _whiteListed = new DataDictionary();

        [Header("Configuration")]
        [Tooltip("If true players are treated as blacklisted unless they are in a whitelist")]
        [SerializeField]
        internal bool WhitelistMode;

        [Tooltip(
                "If true and the local player is blacklisted then the local player " +
                "can not blacklist, whitelist or reset any player"
        )]
        [SerializeField]
        internal bool DisallowModifyingWhenBlackListed = true;

#if TLP_DEBUG
        [SerializeField]
        private bool RunTestOnPlayerJoin;
#endif

        #region Udon Lifecycle
        protected virtual void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnEnable));
#endif
            #endregion

            _Initialize();
        }


        protected virtual void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (!Utilities.IsValid(BlackListedPlayers)) {
                _Warn($"{nameof(SyncedEventStringArray)} no longer valid during cleanup");
                return;
            }

            if (!BlackListedPlayers._RemoveListener(this, true)) {
                _Warn(
                        $"{nameof(SyncedEventStringArray)} was not being listened to, " +
                        $"did you remove the {nameof(PlayerBlackList)} already manually?"
                );
            }
        }
        #endregion

        #region Public API
        [PublicAPI]
        public bool _IsBlackListed(string playerName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_IsBlackListed)} '{playerName}'");
#endif
            #endregion


            if (WhitelistMode) {
                return !_whiteListed.ContainsKey(playerName);
            }

            return _blackListed.ContainsKey(playerName) && !_whiteListed.ContainsKey(playerName);
        }

        [PublicAPI]
        public bool _IsWhiteListed(string playerName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_IsWhiteListed)} '{playerName}'");
#endif
            #endregion

            if (WhitelistMode) {
                return _whiteListed.ContainsKey(playerName);
            }

            return _whiteListed.ContainsKey(playerName) || !_blackListed.ContainsKey(playerName);
        }

        /// <summary>
        /// Adds a player to the dynamic blacklist and sends it to other players.
        /// If the player was part of a whitelist the player will be removed from that.
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns>true on success, false on sync errors or invalid name</returns>
        [PublicAPI]
        public bool _AddToBlackList(string playerName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_AddToBlackList)} '{playerName}'");
#endif
            #endregion

            if (string.IsNullOrWhiteSpace(playerName)) {
                _Error($"Empty {nameof(playerName)}");
                return false;
            }

            if (DisallowModifyingWhenBlackListed && _IsBlackListed(Networking.LocalPlayer._DisplayNameSafe())) {
                _Warn($"You are blacklisted and thus not allowed to blacklist player '{playerName}'");
                return false;
            }

            if (_dynamicallyWhiteListedEntries.ContainsKey(playerName)) {
                _dynamicallyWhiteListedEntries.Remove(playerName);
                if (!_UpdateSyncedPlayerList(_dynamicallyWhiteListedEntries.GetKeys(), WhiteListedPlayers)) {
                    _Error($"Failed to remove blacklisted player '{playerName}' from whitelist for everyone");
                    return false;
                }
            }

            if (_dynamicallyBlackListedEntries.ContainsKey(playerName)) {
                // already blacklisted
                if (_UpdateSyncedPlayerList(_dynamicallyBlackListedEntries.GetKeys(), BlackListedPlayers)) {
                    return true;
                }

                _Error($"Failed to add player '{playerName}' to blacklist for everyone");
                return false;
            }

            _dynamicallyBlackListedEntries[playerName] = true;
            if (_UpdateSyncedPlayerList(_dynamicallyBlackListedEntries.GetKeys(), BlackListedPlayers)) {
                return true;
            }

            _Error($"Failed to add player '{playerName}' to blacklist for everyone");
            return false;
        }

        /// <summary>
        /// Adds a player to the dynamic whitelist and sends it to other players.
        /// If the player was part of a blacklist the player will be removed from that.
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns>true on success, false on sync errors or invalid name</returns>
        [PublicAPI]
        public bool _AddToWhiteList(string playerName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_AddToWhiteList)} '{playerName}'");
#endif
            #endregion

            if (string.IsNullOrWhiteSpace(playerName)) {
                _Error($"Empty {nameof(playerName)}");
                return false;
            }

            if (DisallowModifyingWhenBlackListed && _IsBlackListed(Networking.LocalPlayer._DisplayNameSafe())) {
                _Warn($"You are blacklisted and thus not allowed to whitelist player '{playerName}'");
                return false;
            }

            if (_dynamicallyBlackListedEntries.ContainsKey(playerName)) {
                _dynamicallyBlackListedEntries.Remove(playerName);
                if (!_UpdateSyncedPlayerList(_dynamicallyBlackListedEntries.GetKeys(), BlackListedPlayers)) {
                    _Error($"Failed to remove blacklisted player '{playerName}' from whitelist for everyone");
                    return false;
                }
            }

            if (_dynamicallyWhiteListedEntries.ContainsKey(playerName)) {
                // already whitelisted
                if (_UpdateSyncedPlayerList(_dynamicallyWhiteListedEntries.GetKeys(), WhiteListedPlayers)) {
                    return true;
                }

                _Error($"Failed to add player '{playerName}' to whitelist for everyone");
                return false;
            }

            _dynamicallyWhiteListedEntries[playerName] = true;
            if (_UpdateSyncedPlayerList(_dynamicallyWhiteListedEntries.GetKeys(), WhiteListedPlayers)) {
                return true;
            }

            _Error($"Failed to add player '{playerName}' to whitelist for everyone");
            return false;
        }

        /// <summary>
        /// Reverts the player to the initial state loaded from text assets and downloaded lists and
        /// syncs the changed dynamic lists with other players.
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns>true on success (regardless of if name was part of dynamic lists or not),
        /// false on sync errors or invalid name</returns>
        [PublicAPI]
        public bool _ResetToDefault(string playerName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_ResetToDefault)} '{playerName}'");
#endif
            #endregion

            if (string.IsNullOrWhiteSpace(playerName)) {
                _Error($"Empty {nameof(playerName)}");
                return false;
            }

            if (DisallowModifyingWhenBlackListed && _IsBlackListed(Networking.LocalPlayer._DisplayNameSafe())) {
                _Warn($"You are blacklisted and thus not allowed to whitelist player '{playerName}'");
                return false;
            }

            if (_dynamicallyBlackListedEntries.ContainsKey(playerName)) {
                _dynamicallyBlackListedEntries.Remove(playerName);
                if (!_UpdateSyncedPlayerList(_dynamicallyBlackListedEntries.GetKeys(), BlackListedPlayers)) {
                    _Error($"Failed to sync blacklisted player '{playerName}' with other players");
                    return false;
                }
            }

            if (_dynamicallyWhiteListedEntries.ContainsKey(playerName)) {
                _dynamicallyWhiteListedEntries.Remove(playerName);
                if (!_UpdateSyncedPlayerList(_dynamicallyWhiteListedEntries.GetKeys(), BlackListedPlayers)) {
                    _Error($"Failed to sync blacklisted player '{playerName}' with other players");
                    return false;
                }
            }

            return true;
        }
        #endregion

        public override void _OnEvent(string eventName) {
            switch (eventName) {
                case nameof(_OnSharedBlacklistChanged):
                {
                    #region TLP_DEBUG
#if TLP_DEBUG
                    _DebugLog($"{nameof(_OnEvent)} {eventName}");
#endif
                    #endregion

                    _OnSharedBlacklistChanged();
                    break;
                }

                case nameof(_OnSharedWhitelistChanged):
                {
                    #region TLP_DEBUG
#if TLP_DEBUG
                    _DebugLog($"{nameof(_OnEvent)} {eventName}");
#endif
                    #endregion

                    _OnSharedWhitelistChanged();
                    break;
                }
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }

        private void _LoadInitialNames(DataDictionary output, string text) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_LoadInitialNames));
#endif
            #endregion

            output.Clear();

            text = text.Replace("\r\n", "\n");
            string[] entries = text.Split('\n');
            foreach (string playerName in entries) {
                if (string.IsNullOrWhiteSpace(playerName)) {
                    continue;
                }

                if (!output.ContainsKey(playerName)) {
                    output[playerName] = true;
                }
            }
        }

        private void _CombineBlackLists(
                DataDictionary initialBlackListed,
                DataDictionary dynamicBlackListed,
                DataDictionary dynamicWhiteListed,
                DataDictionary outputBlackList
        ) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_CombineBlackLists));
#endif
            #endregion

            outputBlackList.Clear();

            // add initial entries
            var initialNames = initialBlackListed.GetKeys();
            for (int i = 0; i < initialNames.Count; i++) {
                var playerName = initialNames[i];
                if (dynamicWhiteListed.ContainsKey(playerName)) {
                    continue;
                }

                outputBlackList[playerName] = true;
            }

            // add all dynamically blacklisted entries
            var addedNames = dynamicBlackListed.GetKeys();
            for (int i = 0; i < addedNames.Count; i++) {
                var playerName = addedNames[i];
                if (outputBlackList.ContainsKey(playerName)) {
                    continue;
                }

                outputBlackList[playerName] = true;
            }
        }

        private void _CombineWhiteLists(
                DataDictionary initialWhiteListed,
                DataDictionary initialBlackListed,
                DataDictionary dynamicBlackListed,
                DataDictionary dynamicWhiteListed,
                DataDictionary outputWhiteList
        ) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_CombineWhiteLists));
#endif
            #endregion

            outputWhiteList.Clear();

            // add initial entries
            var initialWhite = initialWhiteListed.GetKeys();
            for (int i = 0; i < initialWhite.Count; i++) {
                var playerName = initialWhite[i];
                if (initialBlackListed.ContainsKey(playerName)) {
                    continue;
                }

                if (dynamicBlackListed.ContainsKey(playerName)) {
                    continue;
                }

                outputWhiteList[playerName] = true;
            }

            // add dynamic entries
            var dynamicWhite = dynamicWhiteListed.GetKeys();
            for (int i = 0; i < dynamicWhite.Count; i++) {
                var playerName = dynamicWhite[i];
                if (dynamicBlackListed.ContainsKey(playerName)) {
                    // keep black listed
                    continue;
                }

                if (outputWhiteList.ContainsKey(playerName)) {
                    continue;
                }

                outputWhiteList[playerName] = true;
            }
        }

        private void _OnSharedBlacklistChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnSharedBlacklistChanged));
#endif
            #endregion

            _RefreshReceivedSharedList(_dynamicallyBlackListedEntries, BlackListedPlayers);
            _RebuildResultLists();
        }

        private void _OnSharedWhitelistChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnSharedWhitelistChanged));
#endif
            #endregion

            _RefreshReceivedSharedList(_dynamicallyWhiteListedEntries, WhiteListedPlayers);
            _RebuildResultLists();
        }

        private void _RefreshReceivedSharedList(DataDictionary sharedList, SyncedEventStringArray syncedList) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RefreshReceivedSharedList));
#endif
            #endregion

            sharedList.Clear();
            string[] syncedListValues = syncedList.WorkingValues;
            if (syncedListValues._LengthSafe() == 0) {
                return;
            }

            foreach (string playerName in syncedListValues) {
                if (string.IsNullOrWhiteSpace(playerName)
                    || sharedList.ContainsKey(playerName)) {
                    continue;
                }

                sharedList[playerName] = true;
            }
        }

        private void _RebuildResultLists() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RebuildResultLists));
#endif
            #endregion

            _CombineBlackLists(
                    _initialBlackListedEntries,
                    _dynamicallyBlackListedEntries,
                    _dynamicallyWhiteListedEntries,
                    _blackListed
            );

            _CombineWhiteLists(
                    _initialWhiteListedEntries,
                    _initialBlackListedEntries,
                    _dynamicallyBlackListedEntries,
                    _dynamicallyWhiteListedEntries,
                    _whiteListed
            );
        }

        private bool _UpdateSyncedPlayerList(DataList playerNames, SyncedEventStringArray syncedNames) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_UpdateSyncedPlayerList));
#endif
            #endregion

            var names = playerNames;
            syncedNames.WorkingValues = new string[names.Count];
            for (int i = 0; i < names.Count; i++) {
                syncedNames.WorkingValues[i] = names[i].String;
            }

            if (syncedNames._TakeOwnership()) {
                return syncedNames._Raise(this);
            }

            _Error($"Failed to take ownership");
            return false;
        }

        internal void _Initialize() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Initialize));
#endif
            #endregion


            if (!Utilities.IsValid(BlackListedPlayers)) {
                _ErrorAndDisableComponent($"{nameof(BlackListedPlayers)} is not set");
                return;
            }

            if (!Utilities.IsValid(WhiteListedPlayers)) {
                _ErrorAndDisableComponent($"{nameof(WhiteListedPlayers)} is not set");
                return;
            }

            BlackListedPlayers.ListenerMethod = nameof(_OnSharedBlacklistChanged);
            if (!BlackListedPlayers._AddListenerVerified(this, nameof(_OnSharedBlacklistChanged))) {
                _ErrorAndDisableComponent($"Failed listening to {nameof(SyncedEventStringArray)} change event");
                return;
            }

            WhiteListedPlayers.ListenerMethod = nameof(_OnSharedWhitelistChanged);
            if (!WhiteListedPlayers._AddListenerVerified(this, nameof(_OnSharedWhitelistChanged))) {
                _ErrorAndDisableComponent($"Failed listening to {nameof(SyncedEventStringArray)} change event");
                return;
            }

            if (Utilities.IsValid(OptionalInitialBlackListedPlayerNames)) {
                _LoadInitialNames(
                        _initialBlackListedEntries,
                        OptionalInitialBlackListedPlayerNames.text
                );
            } else {
                _LoadInitialNames(_initialBlackListedEntries, "");
            }

            if (Utilities.IsValid(OptionalInitialWhiteListedPlayerNames)) {
                _LoadInitialNames(
                        _initialWhiteListedEntries,
                        OptionalInitialWhiteListedPlayerNames.text
                );
            } else {
                _LoadInitialNames(_initialWhiteListedEntries, "");
            }

            _RefreshReceivedSharedList(_dynamicallyWhiteListedEntries, WhiteListedPlayers);
            _RefreshReceivedSharedList(_dynamicallyBlackListedEntries, BlackListedPlayers);

            _RebuildResultLists();

            if (OptionalWhiteListUrl != null && !string.IsNullOrEmpty(OptionalWhiteListUrl.ToString())) {
                VRCStringDownloader.LoadUrl(OptionalWhiteListUrl, gameObject.GetComponent<UdonBehaviour>());
            }

            if (OptionalBlackListUrl != null && !string.IsNullOrEmpty(OptionalBlackListUrl.ToString())) {
                VRCStringDownloader.LoadUrl(OptionalBlackListUrl, gameObject.GetComponent<UdonBehaviour>());
            }
        }


        #region Callbacks
        public override void OnStringLoadSuccess(IVRCStringDownload result) {
            base.OnStringLoadSuccess(result);

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnStringLoadSuccess)} {result.Url}\n{result.Result}");
#endif
            #endregion


            if (result.Url.ToString() == OptionalWhiteListUrl.ToString()) {
                _DebugLog("Updating initial whitelist using downloaded result");
                string initial = Utilities.IsValid(OptionalInitialWhiteListedPlayerNames)
                        ? OptionalInitialWhiteListedPlayerNames.text
                        : "";
                _LoadInitialNames(
                        _initialWhiteListedEntries,
                        $"{initial}\n{(string.IsNullOrEmpty(result.Result) ? "" : result.Result)}"
                );
            }

            if (result.Url.ToString() == OptionalBlackListUrl.ToString()) {
                _DebugLog("Updating initial blacklist using downloaded result");
                string initial = Utilities.IsValid(OptionalInitialBlackListedPlayerNames)
                        ? OptionalInitialBlackListedPlayerNames.text
                        : "";
                _LoadInitialNames(
                        _initialBlackListedEntries,
                        $"{initial}\n{(string.IsNullOrEmpty(result.Result) ? "" : result.Result)}"
                );
            }

            _RebuildResultLists();
        }

        public override void OnStringLoadError(IVRCStringDownload result) {
            base.OnStringLoadError(result);

            _Error($"{nameof(OnStringLoadError)} {result.Url} {result.Error}");

            if (result.Url.ToString() == OptionalWhiteListUrl.ToString()
                && !string.IsNullOrEmpty(OptionalWhiteListUrl.ToString())
                && result.ErrorCode != 404) {
                VRCStringDownloader.LoadUrl(OptionalWhiteListUrl, gameObject.GetComponent<UdonBehaviour>());
            }

            if (result.Url.ToString() == OptionalBlackListUrl.ToString()
                && !string.IsNullOrEmpty(OptionalBlackListUrl.ToString())
                && result.ErrorCode != 404) {
                VRCStringDownloader.LoadUrl(OptionalBlackListUrl, gameObject.GetComponent<UdonBehaviour>());
            }
        }
        #endregion

#if TLP_DEBUG
        public override void OnPlayerJoined(VRCPlayerApi player) {
            base.OnPlayerJoined(player);
            if (!RunTestOnPlayerJoin) {
                return;
            }

            _DebugLog($"{nameof(OnPlayerJoined)} {player._DisplayNameSafe()}");

            if (_IsBlackListed(player._DisplayNameSafe())) {
                _DebugLog($"{player._DisplayNameSafe()} is blacklisted by {name}");
                _Assert(_AddToWhiteList(player._DisplayNameSafe()), "Failed to whitelist player");
                _Assert(_IsWhiteListed(player._DisplayNameSafe()), "Player not whitelisted after manually adding");
                _Assert(_ResetToDefault(player._DisplayNameSafe()), "Failed to reset player");
                _Assert(_IsBlackListed(player._DisplayNameSafe()), "Player not blacklisted again after resetting");
            }

            if (_IsWhiteListed(player._DisplayNameSafe())) {
                _DebugLog($"{player._DisplayNameSafe()} is whitelisted by {name}");
                _Assert(_AddToBlackList(player._DisplayNameSafe()), "Failed to blacklist player");
                _Assert(_IsBlackListed(player._DisplayNameSafe()), "Player not blacklisted after manually adding");
                _Assert(_ResetToDefault(player._DisplayNameSafe()), "Failed to reset player");
                _Assert(_IsWhiteListed(player._DisplayNameSafe()), "Player not whitelisted again after resetting");
            }
        }
#endif
    }
}