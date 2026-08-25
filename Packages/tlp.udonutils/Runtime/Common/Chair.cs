using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sync;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(Chair), ExecutionOrder)]
    public class Chair : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = ChairProxy.ExecutionOrder + 1;

        [FormerlySerializedAs("chairProxy")]
        [SerializeField]
        internal ChairProxy ChairProxy;

        private VRCStation _station;
        private bool _localPlayerSitting;
        private bool _remotePlayerSitting;

        [FormerlySerializedAs("onLocalPlayerEntered")]
        [Header("Events")]
        public UdonEvent OnLocalPlayerEntered;

        [FormerlySerializedAs("onLocalPlayerExited")]
        public UdonEvent OnLocalPlayerExited;

        [FormerlySerializedAs("onRemotePlayerEntered")]
        public UdonEvent OnRemotePlayerEntered;

        [FormerlySerializedAs("onRemotePlayerExited")]
        public UdonEvent OnRemotePlayerExited;

        #region UdonSharp Lifecycle
        internal void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (Utilities.IsValid(_station) && _localPlayerSitting) {
                _station.ExitStation(Networking.LocalPlayer);
                _localPlayerSitting = false;
            }
        }

        internal void Update() {
            if (!_localPlayerSitting || !Utilities.IsValid(_station)) {
                return;
            }

            if (Networking.IsOwner(_station.gameObject)) {
                // everything ok, local player may stay seated
                return;
            }

            // another player is also sitting but has authority over the station
            _station.ExitStation(Networking.LocalPlayer);
            _localPlayerSitting = false;
        }
        #endregion

        #region Interaction Events
        public override void Interact() {
#if TLP_DEBUG
            _DebugLog(nameof(Interact));
#endif

            if (NetworkCalling.InNetworkCall) {
                _Warn($"{nameof(Interact)}: called during network call, skipping");
                return;
            }
            
            if (!enabled) {
                _Warn("Not enabled");
                return;
            }

            var go = gameObject;
            OwnershipTransfer._TransferOwnershipFrom(
                    go,
                    go,
                    Networking.LocalPlayer,
                    false
            );

            _station.UseStation(Networking.LocalPlayer);
        }
        #endregion

        #region VRC Station Events
        public override void OnStationEntered(VRCPlayerApi player) {
#if TLP_DEBUG
            _DebugLog(nameof(OnStationEntered));
#endif
            if (!enabled) {
                _Warn("Not enabled");
                return;
            }

            if (!Utilities.IsValid(player)) {
                _Error("Invalid player entered station");
                return;
            }

            _DebugLog($"{player._ToStringSafe()} entered Chair");

            if (player.isLocal) {
                _localPlayerSitting = true;
                _NotifyLocalPlayerEntered();
                return;
            }

            _remotePlayerSitting = true;
            _NotifyRemotePlayerEntered(player);
        }

        public override void OnStationExited(VRCPlayerApi player) {
#if TLP_DEBUG
            _DebugLog(nameof(OnStationExited));
#endif
            if (!enabled) {
                _Warn("Not enabled");
                return;
            }

            if (!Utilities.IsValid(player)) {
                _Error($"{nameof(player)} invalid");
                return;
            }

            _DebugLog($"{nameof(OnStationExited)}:{player.displayName}({player.playerId}) exited Chair");

            if (player.isLocal) {
                _localPlayerSitting = false;
                _NotifyLocalPlayerExited();

                return;
            }

            _remotePlayerSitting = false;
            _NotifyRemotePlayerExited(player);
        }
        #endregion

        #region Public API
        public VRCPlayerApi _GetSeatedPlayer() {
            return _localPlayerSitting || _remotePlayerSitting ? Networking.GetOwner(gameObject) : null;
        }
        #endregion

        #region Local Player Events
        private void _NotifyLocalPlayerEntered() {
#if TLP_DEBUG
            _DebugLog(nameof(_NotifyLocalPlayerEntered));
#endif

            if (Utilities.IsValid(ChairProxy) && !ChairProxy._OnLocalPlayerEntered()) {
                _Warn($"Chair proxy could not process {nameof(ChairProxy._OnLocalPlayerEntered)}");
            }

            if (!Utilities.IsValid(OnLocalPlayerEntered)) {
                _Warn($"{nameof(OnLocalPlayerEntered)} not set, event will not be raised");
            } else {
                OnLocalPlayerEntered._Raise(this);
            }
        }

        private void _NotifyLocalPlayerExited() {
#if TLP_DEBUG
            _DebugLog(nameof(_NotifyLocalPlayerExited));
#endif
            if (Utilities.IsValid(ChairProxy) && !ChairProxy._OnLocalPlayerExited()) {
                _Warn($"Chair proxy could not process {nameof(ChairProxy._OnLocalPlayerExited)}");
            }

            if (!Utilities.IsValid(OnLocalPlayerExited)) {
                _Warn($"{nameof(OnLocalPlayerExited)} not set, event will not be raised");
            } else {
                OnLocalPlayerExited._Raise(this);
            }
        }
        #endregion

        #region Remote Player Events
        private void _NotifyRemotePlayerEntered(VRCPlayerApi player) {
#if TLP_DEBUG
            _DebugLog(nameof(_NotifyRemotePlayerEntered));
#endif

            if (Utilities.IsValid(ChairProxy) && !ChairProxy._OnRemotePlayerEntered(player)) {
                _Warn($"Chair proxy could not process {nameof(ChairProxy._OnRemotePlayerEntered)}");
            }

            if (!Utilities.IsValid(OnRemotePlayerEntered)) {
                _Warn($"{nameof(OnRemotePlayerEntered)} not set, event will not be raised");
            } else {
                OnRemotePlayerEntered._Raise(this);
            }
        }

        private void _NotifyRemotePlayerExited(VRCPlayerApi player) {
#if TLP_DEBUG
            _DebugLog(nameof(_NotifyRemotePlayerExited));
#endif
            if (Utilities.IsValid(ChairProxy) && !ChairProxy._OnRemotePlayerExited(player)) {
                _Warn($"Chair proxy could not process {nameof(ChairProxy._OnRemotePlayerExited)}");
            }

            if (!Utilities.IsValid(OnRemotePlayerExited)) {
                _Warn($"{nameof(OnRemotePlayerExited)} not set, event will not be raised");
            } else {
                OnRemotePlayerExited._Raise(this);
            }
        }
        #endregion

        #region Hook Implementations
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            _station = (VRCStation)gameObject.GetComponent(typeof(VRCStation));
            if (!Utilities.IsValid(_station)) {
                _Error($"{name} is missing a {nameof(VRCStation)} component");
                return false;
            }

            return true;
        }
        #endregion
    }
}