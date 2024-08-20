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
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlayerBlackList : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DefaultStart;

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
            DebugLog(nameof(OnEnable));
#endif
            #endregion

            Initialize();
        }


        protected virtual void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (!Utilities.IsValid(BlackListedPlayers)) {
                Warn($"{nameof(SyncedEventStringArray)} no longer valid during cleanup");
                return;
            }

            if (!BlackListedPlayers.RemoveListener(this, true)) {
                Warn(
                        $"{nameof(SyncedEventStringArray)} was not being listened to, " +
                        $"did you remove the {nameof(PlayerBlackList)} already manually?"
                );
            }
        }
        #endregion

        #region Public API
        [PublicAPI]
        public bool IsBlackListed(string playerName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(IsBlackListed)} '{playerName}'");
#endif
            #endregion


            if (WhitelistMode) {
                return !_whiteListed.ContainsKey(playerName);
            }

            return _blackListed.ContainsKey(playerName) && !_whiteListed.ContainsKey(playerName);
        }

        [PublicAPI]
        public bool IsWhiteListed(string playerName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(IsWhiteListed)} '{playerName}'");
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
        public bool AddToBlackList(string playerName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(AddToBlackList)} '{playerName}'");
#endif
            #endregion

            if (string.IsNullOrWhiteSpace(playerName)) {
                Error($"Empty {nameof(playerName)}");
                return false;
            }

            if (DisallowModifyingWhenBlackListed
                && IsBlackListed(Networking.LocalPlayer.DisplayNameSafe())) {
                Warn($"You are blacklisted and thus not allowed to blacklist player '{playerName}'");
                return false;
            }

            if (_dynamicallyWhiteListedEntries.ContainsKey(playerName)) {
                _dynamicallyWhiteListedEntries.Remove(playerName);
                if (!UpdateSyncedPlayerList(_dynamicallyWhiteListedEntries.GetKeys(), WhiteListedPlayers)) {
                    Error($"Failed to remove blacklisted player '{playerName}' from whitelist for everyone");
                    return false;
                }
            }

            if (_dynamicallyBlackListedEntries.ContainsKey(playerName)) {
                // already blacklisted
                if (UpdateSyncedPlayerList(_dynamicallyBlackListedEntries.GetKeys(), BlackListedPlayers)) {
                    return true;
                }

                Error($"Failed to add player '{playerName}' to blacklist for everyone");
                return false;
            }

            _dynamicallyBlackListedEntries[playerName] = true;
            if (UpdateSyncedPlayerList(_dynamicallyBlackListedEntries.GetKeys(), BlackListedPlayers)) {
                return true;
            }

            Error($"Failed to add player '{playerName}' to blacklist for everyone");
            return false;
        }

        /// <summary>
        /// Adds a player to the dynamic whitelist and sends it to other players.
        /// If the player was part of a blacklist the player will be removed from that.
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns>true on success, false on sync errors or invalid name</returns>
        [PublicAPI]
        public bool AddToWhiteList(string playerName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(AddToWhiteList)} '{playerName}'");
#endif
            #endregion

            if (string.IsNullOrWhiteSpace(playerName)) {
                Error($"Empty {nameof(playerName)}");
                return false;
            }

            if (DisallowModifyingWhenBlackListed && IsBlackListed(Networking.LocalPlayer.DisplayNameSafe())) {
                Warn($"You are blacklisted and thus not allowed to whitelist player '{playerName}'");
                return false;
            }

            if (_dynamicallyBlackListedEntries.ContainsKey(playerName)) {
                _dynamicallyBlackListedEntries.Remove(playerName);
                if (!UpdateSyncedPlayerList(_dynamicallyBlackListedEntries.GetKeys(), BlackListedPlayers)) {
                    Error($"Failed to remove blacklisted player '{playerName}' from whitelist for everyone");
                    return false;
                }
            }

            if (_dynamicallyWhiteListedEntries.ContainsKey(playerName)) {
                // already whitelisted
                if (UpdateSyncedPlayerList(_dynamicallyWhiteListedEntries.GetKeys(), WhiteListedPlayers)) {
                    return true;
                }

                Error($"Failed to add player '{playerName}' to whitelist for everyone");
                return false;
            }

            _dynamicallyWhiteListedEntries[playerName] = true;
            if (UpdateSyncedPlayerList(_dynamicallyWhiteListedEntries.GetKeys(), WhiteListedPlayers)) {
                return true;
            }

            Error($"Failed to add player '{playerName}' to whitelist for everyone");
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
        public bool ResetToDefault(string playerName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(ResetToDefault)} '{playerName}'");
#endif
            #endregion

            if (string.IsNullOrWhiteSpace(playerName)) {
                Error($"Empty {nameof(playerName)}");
                return false;
            }

            if (DisallowModifyingWhenBlackListed && IsBlackListed(Networking.LocalPlayer.DisplayNameSafe())) {
                Warn($"You are blacklisted and thus not allowed to whitelist player '{playerName}'");
                return false;
            }

            if (_dynamicallyBlackListedEntries.ContainsKey(playerName)) {
                _dynamicallyBlackListedEntries.Remove(playerName);
                if (!UpdateSyncedPlayerList(_dynamicallyBlackListedEntries.GetKeys(), BlackListedPlayers)) {
                    Error($"Failed to sync blacklisted player '{playerName}' with other players");
                    return false;
                }
            }

            if (_dynamicallyWhiteListedEntries.ContainsKey(playerName)) {
                _dynamicallyWhiteListedEntries.Remove(playerName);
                if (!UpdateSyncedPlayerList(_dynamicallyWhiteListedEntries.GetKeys(), BlackListedPlayers)) {
                    Error($"Failed to sync blacklisted player '{playerName}' with other players");
                    return false;
                }
            }

            return true;
        }
        #endregion

        public override void OnEvent(string eventName) {
            switch (eventName) {
                case nameof(OnSharedBlacklistChanged):
                {
                    #region TLP_DEBUG
#if TLP_DEBUG
                    DebugLog($"{nameof(OnEvent)} {eventName}");
#endif
                    #endregion

                    OnSharedBlacklistChanged();
                    break;
                }

                case nameof(OnSharedWhitelistChanged):
                {
                    #region TLP_DEBUG
#if TLP_DEBUG
                    DebugLog($"{nameof(OnEvent)} {eventName}");
#endif
                    #endregion

                    OnSharedWhitelistChanged();
                    break;
                }
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }

        private void LoadInitialNames(DataDictionary output, string text) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(LoadInitialNames));
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

        private void CombineBlackLists(
                DataDictionary initialBlackListed,
                DataDictionary dynamicBlackListed,
                DataDictionary dynamicWhiteListed,
                DataDictionary outputBlackList
        ) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CombineBlackLists));
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

        private void CombineWhiteLists(
                DataDictionary initialWhiteListed,
                DataDictionary initialBlackListed,
                DataDictionary dynamicBlackListed,
                DataDictionary dynamicWhiteListed,
                DataDictionary outputWhiteList
        ) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CombineWhiteLists));
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

        private void OnSharedBlacklistChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnSharedBlacklistChanged));
#endif
            #endregion

            RefreshReceivedSharedList(_dynamicallyBlackListedEntries, BlackListedPlayers);
            RebuildResultLists();
        }

        private void OnSharedWhitelistChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnSharedWhitelistChanged));
#endif
            #endregion

            RefreshReceivedSharedList(_dynamicallyWhiteListedEntries, WhiteListedPlayers);
            RebuildResultLists();
        }

        private void RefreshReceivedSharedList(DataDictionary sharedList, SyncedEventStringArray syncedList) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RefreshReceivedSharedList));
#endif
            #endregion

            sharedList.Clear();
            string[] syncedListValues = syncedList.WorkingValues;
            if (syncedListValues.LengthSafe() == 0) {
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

        private void RebuildResultLists() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RebuildResultLists));
#endif
            #endregion

            CombineBlackLists(
                    _initialBlackListedEntries,
                    _dynamicallyBlackListedEntries,
                    _dynamicallyWhiteListedEntries,
                    _blackListed
            );

            CombineWhiteLists(
                    _initialWhiteListedEntries,
                    _initialBlackListedEntries,
                    _dynamicallyBlackListedEntries,
                    _dynamicallyWhiteListedEntries,
                    _whiteListed
            );
        }

        private bool UpdateSyncedPlayerList(DataList playerNames, SyncedEventStringArray syncedNames) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(UpdateSyncedPlayerList));
#endif
            #endregion

            var names = playerNames;
            syncedNames.WorkingValues = new string[names.Count];
            for (int i = 0; i < names.Count; i++) {
                syncedNames.WorkingValues[i] = names[i].String;
            }

            if (syncedNames.TakeOwnership()) {
                return syncedNames.Raise(this);
            }

            Error($"Failed to take ownership");
            return false;
        }

        internal void Initialize() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Initialize));
#endif
            #endregion


            if (!Utilities.IsValid(BlackListedPlayers)) {
                ErrorAndDisableComponent($"{nameof(BlackListedPlayers)} is not set");
                return;
            }

            if (!Utilities.IsValid(WhiteListedPlayers)) {
                ErrorAndDisableComponent($"{nameof(WhiteListedPlayers)} is not set");
                return;
            }

            BlackListedPlayers.ListenerMethod = nameof(OnSharedBlacklistChanged);
            if (!BlackListedPlayers.AddListenerVerified(this, nameof(OnSharedBlacklistChanged))) {
                ErrorAndDisableComponent($"Failed listening to {nameof(SyncedEventStringArray)} change event");
                return;
            }

            WhiteListedPlayers.ListenerMethod = nameof(OnSharedWhitelistChanged);
            if (!WhiteListedPlayers.AddListenerVerified(this, nameof(OnSharedWhitelistChanged))) {
                ErrorAndDisableComponent($"Failed listening to {nameof(SyncedEventStringArray)} change event");
                return;
            }

            if (Utilities.IsValid(OptionalInitialBlackListedPlayerNames)) {
                LoadInitialNames(
                        _initialBlackListedEntries,
                        OptionalInitialBlackListedPlayerNames.text
                );
            } else {
                LoadInitialNames(_initialBlackListedEntries, "");
            }

            if (Utilities.IsValid(OptionalInitialWhiteListedPlayerNames)) {
                LoadInitialNames(
                        _initialWhiteListedEntries,
                        OptionalInitialWhiteListedPlayerNames.text
                );
            } else {
                LoadInitialNames(_initialWhiteListedEntries, "");
            }

            RefreshReceivedSharedList(_dynamicallyWhiteListedEntries, WhiteListedPlayers);
            RefreshReceivedSharedList(_dynamicallyBlackListedEntries, BlackListedPlayers);

            RebuildResultLists();

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
            DebugLog($"{nameof(OnStringLoadSuccess)} {result.Url}\n{result.Result}");
#endif
            #endregion


            if (result.Url.ToString() == OptionalWhiteListUrl.ToString()) {
                DebugLog("Updating initial whitelist using downloaded result");
                string initial = Utilities.IsValid(OptionalInitialWhiteListedPlayerNames)
                        ? OptionalInitialWhiteListedPlayerNames.text
                        : "";
                LoadInitialNames(
                        _initialWhiteListedEntries,
                        $"{initial}\n{(string.IsNullOrEmpty(result.Result) ? "" : result.Result)}"
                );
            }

            if (result.Url.ToString() == OptionalBlackListUrl.ToString()) {
                DebugLog("Updating initial blacklist using downloaded result");
                string initial = Utilities.IsValid(OptionalInitialBlackListedPlayerNames)
                        ? OptionalInitialBlackListedPlayerNames.text
                        : "";
                LoadInitialNames(
                        _initialBlackListedEntries,
                        $"{initial}\n{(string.IsNullOrEmpty(result.Result) ? "" : result.Result)}"
                );
            }

            RebuildResultLists();
        }

        public override void OnStringLoadError(IVRCStringDownload result) {
            base.OnStringLoadError(result);

            Error($"{nameof(OnStringLoadError)} {result.Url} {result.Error}");

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

            DebugLog($"{nameof(OnPlayerJoined)} {player.DisplayNameSafe()}");

            if (IsBlackListed(player.DisplayNameSafe())) {
                DebugLog($"{player.DisplayNameSafe()} is blacklisted by {name}");
                Assert(AddToWhiteList(player.DisplayNameSafe()), "Failed to whitelist player");
                Assert(IsWhiteListed(player.DisplayNameSafe()), "Player not whitelisted after manually adding");
                Assert(ResetToDefault(player.DisplayNameSafe()), "Failed to reset player");
                Assert(IsBlackListed(player.DisplayNameSafe()), "Player not blacklisted again after resetting");
            }

            if (IsWhiteListed(player.DisplayNameSafe())) {
                DebugLog($"{player.DisplayNameSafe()} is whitelisted by {name}");
                Assert(AddToBlackList(player.DisplayNameSafe()), "Failed to blacklist player");
                Assert(IsBlackListed(player.DisplayNameSafe()), "Player not blacklisted after manually adding");
                Assert(ResetToDefault(player.DisplayNameSafe()), "Failed to reset player");
                Assert(IsWhiteListed(player.DisplayNameSafe()), "Player not whitelisted again after resetting");
            }
        }
#endif
    }
}