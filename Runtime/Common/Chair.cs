using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Sync;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Common
{
    [RequireComponent(typeof(VRC.SDK3.Components.VRCStation))]
    public class Chair : TlpBaseBehaviour
    {
        [SerializeField]
        internal ChairProxy chairProxy;

        internal VRCStation Station;
        private bool _localPlayerSitting;
        private bool _remotePlayerSitting;

        [Header("Events")]
        public UdonEvent onLocalPlayerEntered;

        public UdonEvent onLocalPlayerExited;
        public UdonEvent onRemotePlayerEntered;
        public UdonEvent onRemotePlayerExited;

        public override void Interact()
        {
#if TLP_DEBUG
            DebugLog(nameof(Interact));
#endif
            Station = (VRCStation)GetComponent(typeof(VRCStation));
            if (!Utilities.IsValid(Station))
            {
                Error("Station invalid");
                return;
            }

            var go = gameObject;
            OwnershipTransfer.TransferOwnershipFrom(
                go,
                go,
                Networking.LocalPlayer,
                false
            );

            Station.UseStation(Networking.LocalPlayer);
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
#if TLP_DEBUG
            DebugLog(nameof(OnStationEntered));
#endif

            if (!Assert(Utilities.IsValid(player), "Player invalid", this))
            {
                return;
            }

            DebugLog($"{nameof(OnStationEntered)}:{player.displayName}({player.playerId}) entered Chair");

            if (player.isLocal)
            {
                _localPlayerSitting = true;
                NotifyLocalPlayerEntered();

                return;
            }

            _remotePlayerSitting = true;
            NotifyRemotePlayerEntered(player);
        }

        private void NotifyRemotePlayerEntered(VRCPlayerApi player)
        {
#if TLP_DEBUG
            DebugLog(nameof(NotifyRemotePlayerEntered));
#endif

            if (!Utilities.IsValid(onRemotePlayerEntered))
            {
                Warn("onRemotePlayerEntered not set, event will not be raised");
            }
            else
            {
                onRemotePlayerEntered.Raise(this);
            }

            if (Utilities.IsValid(chairProxy) && !chairProxy.OnRemotePlayerEntered(player))
            {
                Warn($"Chair proxy could not process {nameof(chairProxy.OnRemotePlayerEntered)}");
            }
        }

        private void NotifyLocalPlayerEntered()
        {
#if TLP_DEBUG
            DebugLog(nameof(NotifyLocalPlayerEntered));
#endif

            if (!Utilities.IsValid(onLocalPlayerEntered))
            {
                Warn("onLocalPlayerEntered not set, event will not be raised");
            }
            else
            {
                onLocalPlayerEntered.Raise(this);
            }

            if (Utilities.IsValid(chairProxy) && !chairProxy.OnLocalPlayerEntered())
            {
                Warn($"Chair proxy could not process {nameof(chairProxy.OnLocalPlayerEntered)}");
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
#if TLP_DEBUG
            DebugLog(nameof(OnStationExited));
#endif

            if (!Assert(Utilities.IsValid(player), "Player invalid", this))
            {
                return;
            }

            DebugLog($"{nameof(OnStationExited)}:{player.displayName}({player.playerId}) exited Chair");

            if (player.isLocal)
            {
                _localPlayerSitting = false;
                NotifyLocalPlayerExited();

                return;
            }

            _remotePlayerSitting = false;
            NotifyRemotePlayerExited(player);
        }

        private void NotifyRemotePlayerExited(VRCPlayerApi player)
        {
#if TLP_DEBUG
            DebugLog(nameof(NotifyRemotePlayerExited));
#endif
            if (!Utilities.IsValid(onRemotePlayerExited))
            {
                Warn("onRemotePlayerExited not set, event will not be raised");
            }
            else
            {
                onRemotePlayerExited.Raise(this);
            }

            if (Utilities.IsValid(chairProxy) && !chairProxy.OnRemotePlayerExited(player))
            {
                Warn($"Chair proxy could not process {nameof(chairProxy.OnRemotePlayerExited)}");
            }
        }

        private void NotifyLocalPlayerExited()
        {
#if TLP_DEBUG
            DebugLog(nameof(NotifyLocalPlayerExited));
#endif
            if (!Utilities.IsValid(onLocalPlayerExited))
            {
                Warn("onLocalPlayerExited not set, event will not be raised");
            }
            else
            {
                onLocalPlayerExited.Raise(this);
            }

            if (Utilities.IsValid(chairProxy) && !chairProxy.OnLocalPlayerExited())
            {
                Warn($"Chair proxy could not process {nameof(chairProxy.OnLocalPlayerExited)}");
            }
        }

        public void Update()
        {
            if (_localPlayerSitting && Utilities.IsValid(Station))
            {
                if (Networking.IsOwner(Station.gameObject))
                {
                    // everything ok, local player may stay seated
                    return;
                }

                // another player is also sitting but has authority over the station
                Station.ExitStation(Networking.LocalPlayer);
                _localPlayerSitting = false;
            }
        }

        [PublicAPI]
        public VRCPlayerApi GetSeatedPlayer()
        {
            return _localPlayerSitting || _remotePlayerSitting ? Networking.GetOwner(gameObject) : null;
        }
    }
}