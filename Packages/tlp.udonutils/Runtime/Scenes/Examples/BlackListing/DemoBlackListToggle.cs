using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Player;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Scenes.Examples.BlackListing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DemoBlackListToggle), ExecutionOrder)]
    public class DemoBlackListToggle : Controller
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = ImageDownloader.ExecutionOrder + 1;

        public Button WhiteListButton;
        public Button BlackListButton;

        public PlayerBlackList PlayerBlackList;

        internal void OnEnable() {

            if (!Utilities.IsValid(WhiteListButton)) {
                _ErrorAndDisableGameObject($"{nameof(WhiteListButton)} is not set");
                return;
            }

            if (!Utilities.IsValid(BlackListButton)) {
                _ErrorAndDisableGameObject($"{nameof(BlackListButton)} is not set");
                return;
            }

            WhiteListButton.gameObject.SetActive(
                    PlayerBlackList._IsBlackListed(Networking.LocalPlayer._DisplayNameSafe()));
            BlackListButton.gameObject.SetActive(
                    PlayerBlackList._IsWhiteListed(Networking.LocalPlayer._DisplayNameSafe()));
        }

        [PublicAPI]
        public void _AddLocalPlayerToBlackList() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_AddLocalPlayerToBlackList));
#endif
            #endregion

            if (!PlayerBlackList._AddToBlackList(Networking.LocalPlayer._DisplayNameSafe())) {
                _Warn($"Failed to add '{Networking.LocalPlayer._DisplayNameSafe()}' to blacklist");
            }

            OnEnable();
        }

        [PublicAPI]
        public void _AddLocalPlayerToWhiteList() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_AddLocalPlayerToWhiteList));
#endif
            #endregion

            if (!PlayerBlackList._AddToWhiteList(Networking.LocalPlayer._DisplayNameSafe())) {
                _Warn($"Failed to add '{Networking.LocalPlayer._DisplayNameSafe()}' to whitelist");
            }

            OnEnable();
        }
    }
}