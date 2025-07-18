using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// Represents the state of a station in a Unity-based environment using UdonSharp.
/// This class primarily tracks whether the station is occupied and whether the local player
/// is currently in the station.
/// </summary>
/// <remarks>
/// - Can be used directly on a GameObject with a VRCStation on it
/// - or via another scripts VRCStation callbacks.
///   In that case make sure this script is not on that
///   GameObject as well to prevent the callbacks on this
///   script to be called multiple times!
/// </remarks>
[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
[DefaultExecutionOrder(ExecutionOrder)]
[TlpDefaultExecutionOrder(typeof(StationState), ExecutionOrder)]
public class StationState : TlpBaseBehaviour
{
    #region ExecutionOrder
    [PublicAPI]
    public override int ExecutionOrderReadOnly => ExecutionOrder;

    [PublicAPI]
    public new const int ExecutionOrder = TlpExecutionOrder.VehicleMotionStart + 1;
    #endregion

    #region Dependencies
    public VRCStation Station;
    #endregion

    #region State
    public bool IsLocalPlayerInStation { get; private set; }
    public bool IsStationOccupied { get; private set; }
    public VRCPlayerApi PlayerInStation { get; private set; }
    #endregion

    #region PublicApi
    /// <summary>
    /// Validation method to enforce that the StationState is used indirectly by another Script
    /// </summary>
    [PublicAPI]
    public bool ValidateNotOnStationGameObject(VRCStation station) {
        if (!HasStartedOk) {
            return false;
        }

        if (!ReferenceEquals(station, Station)) {
            Error(
                    $"Provided {nameof(VRCStation)} is not own {nameof(Station)}: " +
                    $"{station.GetComponentPathInScene()} != {Station.GetComponentPathInScene()}");
            return false;
        }

        if (!ReferenceEquals(gameObject, Station.gameObject)) {
            return true;
        }

        Error($"{nameof(StationState)} is on the same GameObject as the station: {Station.GetComponentPathInScene()}");
        return false;
    }

    /// <summary>
    /// Validation method to enforce that the StationState is updated directly by the VRCStation
    /// </summary>
    [PublicAPI]
    public bool ValidateOnStationGameObject() {
        if (!HasStartedOk) {
            return false;
        }

        var station = GetComponent<VRCStation>();
        if (Utilities.IsValid(station) && ReferenceEquals(station, Station)) {
            return true;
        }

        Error($"{nameof(StationState)} found no {nameof(VRCStation)} on its own GameObject");
        return false;
    }
    #endregion

    #region Overrides
    protected override bool SetupAndValidate() {
        if (!base.SetupAndValidate()) {
            return false;
        }

        return ValidateDependencies();
    }

    public override void OnStationEntered(VRCPlayerApi player) {
        #region TLP_DEBUG
#if TLP_DEBUG
        DebugLog($"{nameof(OnStationEntered)}: {player.ToStringSafe()}");
#endif
        #endregion

        IsLocalPlayerInStation = player.IsLocalSafe();
        IsStationOccupied = Utilities.IsValid(player);
        PlayerInStation = player;
    }

    public override void OnStationExited(VRCPlayerApi player) {
        #region TLP_DEBUG
#if TLP_DEBUG
        DebugLog($"{nameof(OnStationExited)}: {player.ToStringSafe()}");
#endif
        #endregion

        IsLocalPlayerInStation = false;
        IsStationOccupied = false;
        PlayerInStation = null;
    }
    #endregion

    #region Internal
    private bool ValidateDependencies() {
        if (!IsSet(Station, nameof(Station))) {
            return false;
        }

        return true;
    }
    #endregion
}