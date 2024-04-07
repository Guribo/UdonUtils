using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Events;
using TLP.UdonUtils.Extensions;
using TLP.UdonUtils.Sync;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Common
{
    [RequireComponent(typeof(VRC.SDK3.Components.VRCStation))]
    public class Chair : TlpBaseBehaviour
    {
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
        public void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (Utilities.IsValid(_station) && _localPlayerSitting) {
                _station.ExitStation(Networking.LocalPlayer);
                _localPlayerSitting = false;
            }
        }

        public void Update() {
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
            DebugLog(nameof(Interact));
#endif
            if (!enabled) {
                Warn("Not enabled");
                return;
            }

            var go = gameObject;
            OwnershipTransfer.TransferOwnershipFrom(
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
            DebugLog(nameof(OnStationEntered));
#endif
            if (!enabled) {
                Warn("Not enabled");
                return;
            }

            if (!Utilities.IsValid(player)) {
                Error("Invalid player entered station");
                return;
            }

            DebugLog($"{player.ToStringSafe()} entered Chair");

            if (player.isLocal) {
                _localPlayerSitting = true;
                NotifyLocalPlayerEntered();
                return;
            }

            _remotePlayerSitting = true;
            NotifyRemotePlayerEntered(player);
        }

        public override void OnStationExited(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog(nameof(OnStationExited));
#endif
            if (!enabled) {
                Warn("Not enabled");
                return;
            }

            if (!Assert(Utilities.IsValid(player), "Player invalid", this)) {
                return;
            }

            DebugLog($"{nameof(OnStationExited)}:{player.displayName}({player.playerId}) exited Chair");

            if (player.isLocal) {
                _localPlayerSitting = false;
                NotifyLocalPlayerExited();

                return;
            }

            _remotePlayerSitting = false;
            NotifyRemotePlayerExited(player);
        }
        #endregion


        #region Public API
        public VRCPlayerApi GetSeatedPlayer() {
            return _localPlayerSitting || _remotePlayerSitting ? Networking.GetOwner(gameObject) : null;
        }
        #endregion


        #region Local Player Events
        private void NotifyLocalPlayerEntered() {
#if TLP_DEBUG
            DebugLog(nameof(NotifyLocalPlayerEntered));
#endif

            if (Utilities.IsValid(ChairProxy) && !ChairProxy.OnLocalPlayerEntered()) {
                Warn($"Chair proxy could not process {nameof(ChairProxy.OnLocalPlayerEntered)}");
            }

            if (!Utilities.IsValid(OnLocalPlayerEntered)) {
                Warn($"{nameof(OnLocalPlayerEntered)} not set, event will not be raised");
            } else {
                OnLocalPlayerEntered.Raise(this);
            }
        }

        private void NotifyLocalPlayerExited() {
#if TLP_DEBUG
            DebugLog(nameof(NotifyLocalPlayerExited));
#endif
            if (Utilities.IsValid(ChairProxy) && !ChairProxy.OnLocalPlayerExited()) {
                Warn($"Chair proxy could not process {nameof(ChairProxy.OnLocalPlayerExited)}");
            }

            if (!Utilities.IsValid(OnLocalPlayerExited)) {
                Warn($"{nameof(OnLocalPlayerExited)} not set, event will not be raised");
            } else {
                OnLocalPlayerExited.Raise(this);
            }
        }
        #endregion

        #region Remote Player Events
        private void NotifyRemotePlayerEntered(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog(nameof(NotifyRemotePlayerEntered));
#endif

            if (Utilities.IsValid(ChairProxy) && !ChairProxy.OnRemotePlayerEntered(player)) {
                Warn($"Chair proxy could not process {nameof(ChairProxy.OnRemotePlayerEntered)}");
            }

            if (!Utilities.IsValid(OnRemotePlayerEntered)) {
                Warn($"{nameof(OnRemotePlayerEntered)} not set, event will not be raised");
            } else {
                OnRemotePlayerEntered.Raise(this);
            }
        }

        private void NotifyRemotePlayerExited(VRCPlayerApi player) {
#if TLP_DEBUG
            DebugLog(nameof(NotifyRemotePlayerExited));
#endif
            if (Utilities.IsValid(ChairProxy) && !ChairProxy.OnRemotePlayerExited(player)) {
                Warn($"Chair proxy could not process {nameof(ChairProxy.OnRemotePlayerExited)}");
            }

            if (!Utilities.IsValid(OnRemotePlayerExited)) {
                Warn($"{nameof(OnRemotePlayerExited)} not set, event will not be raised");
            } else {
                OnRemotePlayerExited.Raise(this);
            }
        }
        #endregion


        #region Hook Implementations
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) return false;

            _station = (VRCStation)gameObject.GetComponent(typeof(VRCStation));
            if (!Utilities.IsValid(_station)) {
                Error($"{name} is missing a {nameof(VRCStation)} component");
                return false;
            }

            return true;
        }
        #endregion
    }
}