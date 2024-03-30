﻿using System;
using JetBrains.Annotations;
using TLP.UdonUtils;
using TLP.UdonUtils.Extensions;
using TLP.UdonUtils.Player;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

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