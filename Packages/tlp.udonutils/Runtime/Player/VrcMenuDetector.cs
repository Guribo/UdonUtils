using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Player;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;
#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonUtils.Editor
{
    [CustomEditor(typeof(VrcMenuDetector))]
    public class VrcMenuDetectorEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Detects when the VRChat main menu or quick menu opens/closes and raises events accordingly.";
        }
    }
}
#endif

namespace TLP.UdonUtils.Runtime
{
    public enum MenuState
    {
        Closed = 0,
        QuickMenuOpen = 1,
        MainMenuOpen = 2
    }

    [RequireComponent(typeof(VRCPlayerObject))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(VrcMenuDetector), ExecutionOrder)]
    public class VrcMenuDetector : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = UdonInput.ExecutionOrder + 1;
        #endregion

        #region Constants
        private const int MainMenuLowerThreshold = 6;
        private const int MainMenuUpperThreshold = 15;

        private const int QuickMenuLowerThreshold = 14;
        private const int QuickMenuUpperThreshold = 21;
        #endregion

        #region Dependencies
        [Header("Dependencies")]
        public UdonEvent OnMainMenuOpened;
        public UdonEvent OnMainMenuClosed;
        public UdonEvent OnQuickMenuOpened;
        public UdonEvent OnQuickMenuClosed;
        #endregion

        #region SyncedState
        /// <summary>
        /// See <see cref="MenuState"/>.
        /// </summary>
        [UdonSynced]
        private byte _syncedMenuState;
        #endregion

        #region State
        private readonly Collider[] _colliderArray = new Collider[QuickMenuUpperThreshold];
        private LayerMask _detectionLayerMask;
        #endregion

        #region Public API
        public bool IsMainMenuOpen { get; private set; }
        public bool IsQuickMenuOpen { get; private set; }
        #endregion

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_IsSet(OnMainMenuOpened, nameof(OnMainMenuOpened))) {
                return false;
            }

            if (!_IsSet(OnMainMenuClosed, nameof(OnMainMenuClosed))) {
                return false;
            }

            if (!_IsSet(OnQuickMenuOpened, nameof(OnQuickMenuOpened))) {
                return false;
            }

            if (!_IsSet(OnQuickMenuClosed, nameof(OnQuickMenuClosed))) {
                return false;
            }

            OnMainMenuOpened.ListenerMethod = $"_{nameof(OnMainMenuOpened)}";
            OnMainMenuClosed.ListenerMethod = $"_{nameof(OnMainMenuClosed)}";
            OnQuickMenuOpened.ListenerMethod = $"_{nameof(OnQuickMenuOpened)}";
            OnQuickMenuClosed.ListenerMethod = $"_{nameof(OnQuickMenuClosed)}";

            _detectionLayerMask = LayerMask.GetMask("InternalUI");

            if (!Networking.IsOwner(gameObject)) {
                // only the owner checks for menu state
                enabled = false;
            }

            return true;
        }

        private void Update() {
            if (!VrcCameraSettingUtils.TryGetScreenCameraPosition(out var screenCameraPosition)) {
                return;
            }

            _DetectOpenMenus(screenCameraPosition, out bool mainMenuOpen, out bool quickMenuOpen);
            _RaiseMenuEventsOnChange(mainMenuOpen, quickMenuOpen);
        }

        public override void OnPreSerialization() {
            base.OnPreSerialization();

            if (IsMainMenuOpen) {
                _syncedMenuState = (byte)MenuState.MainMenuOpen;
            } else if (IsQuickMenuOpen) {
                _syncedMenuState = (byte)MenuState.QuickMenuOpen;
            } else {
                _syncedMenuState = (byte)MenuState.Closed;
            }
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);
            switch (_syncedMenuState) {
                case (byte)MenuState.Closed:
                    _RaiseMenuEventsOnChange(false, false);
                    break;
                case (byte)MenuState.QuickMenuOpen:
                    _RaiseMenuEventsOnChange(false, true);
                    break;
                case (byte)MenuState.MainMenuOpen:
                    _RaiseMenuEventsOnChange(true, false);
                    break;
            }
        }

        #region Internal
        private void _DetectOpenMenus(Vector3 screenCameraPosition, out bool mainMenuOpen, out bool quickMenuOpen) {
            int overlaps = UnityEngine.Physics.OverlapSphereNonAlloc(
                    screenCameraPosition,
                    // menu positioning scales with avatar height, 2x as some safety margin
                    2f * LocalPlayer.GetAvatarEyeHeightAsMeters(),
                    _colliderArray,
                    _detectionLayerMask);
            mainMenuOpen = overlaps > MainMenuLowerThreshold && overlaps < MainMenuUpperThreshold;
            quickMenuOpen = overlaps > QuickMenuLowerThreshold && overlaps < QuickMenuUpperThreshold;
        }

        private void _RaiseMenuEventsOnChange(bool mainMenuOpen, bool quickMenuOpen) {
            if (mainMenuOpen != IsMainMenuOpen) {
                IsMainMenuOpen = mainMenuOpen;
                if (Networking.IsOwner(gameObject)) {
                    _MarkNetworkDirty();
                }

                if (IsMainMenuOpen) {
                    _CloseQuickMenuIfNeeded();
                    OnMainMenuOpened._Raise(this);
                    return;
                }

                OnMainMenuClosed._Raise(this);
                return;
            }

            if (quickMenuOpen != IsQuickMenuOpen) {
                IsQuickMenuOpen = quickMenuOpen;
                if (Networking.IsOwner(gameObject)) {
                    _MarkNetworkDirty();
                }

                if (IsQuickMenuOpen) {
                    _CloseMainMenuIfNeeded();
                    OnQuickMenuOpened._Raise(this);
                    return;
                }

                OnQuickMenuClosed._Raise(this);
            }
        }

        private void _CloseQuickMenuIfNeeded() {
            if (!IsQuickMenuOpen) {
                return;
            }

            IsQuickMenuOpen = false;
            OnQuickMenuClosed._Raise(this);
        }

        private void _CloseMainMenuIfNeeded() {
            if (!IsMainMenuOpen) {
                return;
            }

            IsMainMenuOpen = false;
            OnMainMenuClosed._Raise(this);
        }
        #endregion
    }
}