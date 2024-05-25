using JetBrains.Annotations;
using TLP.UdonUtils;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Player;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[DefaultExecutionOrder(ExecutionOrder)]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DemoBlackListToggle : TlpBaseBehaviour
{
    protected override int ExecutionOrderReadOnly => ExecutionOrder;

    [PublicAPI]
    public new const int ExecutionOrder = TlpExecutionOrder.UiStart;

    public Button WhiteListButton;
    public Button BlackListButton;

    public PlayerBlackList PlayerBlackList;

    public void OnEnable() {
        if (!Utilities.IsValid(WhiteListButton)) {
            ErrorAndDisableGameObject($"{nameof(WhiteListButton)} is not set");
            return;
        }

        if (!Utilities.IsValid(BlackListButton)) {
            ErrorAndDisableGameObject($"{nameof(BlackListButton)} is not set");
            return;
        }

        WhiteListButton.gameObject.SetActive(PlayerBlackList.IsBlackListed(Networking.LocalPlayer.DisplayNameSafe()));
        BlackListButton.gameObject.SetActive(PlayerBlackList.IsWhiteListed(Networking.LocalPlayer.DisplayNameSafe()));
    }

    [PublicAPI]
    public void AddLocalPlayerToBlackList() {
        #region TLP_DEBUG
#if TLP_DEBUG
        DebugLog(nameof(AddLocalPlayerToBlackList));
#endif
        #endregion

        if (!PlayerBlackList.AddToBlackList(Networking.LocalPlayer.DisplayNameSafe())) {
            Warn($"Failed to add '{Networking.LocalPlayer.DisplayNameSafe()}' to blacklist");
        }

        OnEnable();
    }

    [PublicAPI]
    public void AddLocalPlayerToWhiteList() {
        #region TLP_DEBUG
#if TLP_DEBUG
        DebugLog(nameof(AddLocalPlayerToWhiteList));
#endif
        #endregion

        if (!PlayerBlackList.AddToWhiteList(Networking.LocalPlayer.DisplayNameSafe())) {
            Warn($"Failed to add '{Networking.LocalPlayer.DisplayNameSafe()}' to whitelist");
        }

        OnEnable();
    }
}