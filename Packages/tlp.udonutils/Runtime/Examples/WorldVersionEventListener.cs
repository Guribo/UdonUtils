using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Examples
{
    /// <summary>
    /// Example of a UdonBehaviour that can get notified when a player with a different world version joined.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(WorldVersionEventListener), ExecutionOrder)]
    public class WorldVersionEventListener : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = WorldVersionCheck.ExecutionOrder + 1;

        #region Dependencies
        public Canvas Canvas;
        public TextMeshProUGUI UpdateAvailableText;
        public TextMeshProUGUI VersionConflictText;

        [Header("Events")]
        [Tooltip("Event raised when a new world update is available")]
        public UdonEvent UpdateAvailableEvent;

        [Tooltip("Event raised when world version conflict between a joining player and the master occurred")]
        public UdonEvent VersionConflictOccurredEvent;
        #endregion

        #region Settings
        public float NotificationTimeout = 20f;
        #endregion

        #region State
        internal float NextUiHidingTime;
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(Canvas)) {
                _ErrorAndDisableGameObject($"{nameof(Canvas)} not set");
                return false;
            }

            if (!Utilities.IsValid(UpdateAvailableText)) {
                _ErrorAndDisableGameObject($"{nameof(UpdateAvailableText)} not set");
                return false;
            }

            if (!Utilities.IsValid(VersionConflictText)) {
                _ErrorAndDisableGameObject($"{nameof(VersionConflictText)} not set");
                return false;
            }

            if (!Utilities.IsValid(UpdateAvailableEvent)) {
                _ErrorAndDisableGameObject($"{nameof(UpdateAvailableEvent)} not set");
                return false;
            }

            if (!Utilities.IsValid(VersionConflictOccurredEvent)) {
                _ErrorAndDisableGameObject($"{nameof(VersionConflictOccurredEvent)} not set");
                return false;
            }

            if (!UpdateAvailableEvent._AddListenerVerified(this, nameof(_WorldUpdateAvailable))) {
                _ErrorAndDisableGameObject($"Failed to listen to {nameof(UpdateAvailableEvent)}");
                return false;
            }

            if (!VersionConflictOccurredEvent._AddListenerVerified(this, nameof(_VersionConflictOccurred))) {
                _ErrorAndDisableGameObject($"Failed to listen to {nameof(VersionConflictOccurredEvent)}");
                return false;
            }

            if (NotificationTimeout <= 0) {
                _Warn(
                        $"{nameof(NotificationTimeout)} <= 0; World version conflict notifications will be shown indefinitely");
            }

            return true;
        }

        public override void _OnEvent(string eventName) {
            switch (eventName) {
                case nameof(_WorldUpdateAvailable):
                    _WorldUpdateAvailable();
                    return;
                case nameof(_VersionConflictOccurred):
                    _VersionConflictOccurred();
                    return;
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }
        #endregion

        [PublicAPI]
        public void _WorldUpdateAvailable() {
            _Warn("World update available!");
            Canvas.gameObject.SetActive(true);

            UpdateAvailableText.gameObject.SetActive(true);

            _ScheduleNotificationTimeout();
        }

        [PublicAPI]
        public void _VersionConflictOccurred() {
            _Warn("World versions differ. Udon Networking might break now.");
            Canvas.gameObject.SetActive(true);
            VersionConflictText.gameObject.SetActive(true);

            if (NotificationTimeout > 0) {
                _ScheduleNotificationTimeout();
            }
        }


        [PublicAPI]
        public void _HideNotifications() {
            if (NextUiHidingTime - Time.timeSinceLevelLoad > 0.1f * NotificationTimeout) {
                return;
            }

            Canvas.gameObject.SetActive(false);
            VersionConflictText.gameObject.SetActive(false);
            UpdateAvailableText.gameObject.SetActive(false);
        }

        #region Internal
        private void _ScheduleNotificationTimeout() {
            NextUiHidingTime = Time.timeSinceLevelLoad + NotificationTimeout;
            SendCustomEventDelayedSeconds(nameof(_HideNotifications), NotificationTimeout);
        }
        #endregion
    }
}