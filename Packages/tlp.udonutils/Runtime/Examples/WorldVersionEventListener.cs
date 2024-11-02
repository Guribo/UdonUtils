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
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

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
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(Canvas)) {
                ErrorAndDisableGameObject($"{nameof(Canvas)} not set");
                return false;
            }

            if (!Utilities.IsValid(UpdateAvailableText)) {
                ErrorAndDisableGameObject($"{nameof(UpdateAvailableText)} not set");
                return false;
            }

            if (!Utilities.IsValid(VersionConflictText)) {
                ErrorAndDisableGameObject($"{nameof(VersionConflictText)} not set");
                return false;
            }

            if (!Utilities.IsValid(UpdateAvailableEvent)) {
                ErrorAndDisableGameObject($"{nameof(UpdateAvailableEvent)} not set");
                return false;
            }

            if (!Utilities.IsValid(VersionConflictOccurredEvent)) {
                ErrorAndDisableGameObject($"{nameof(VersionConflictOccurredEvent)} not set");
                return false;
            }

            if (!UpdateAvailableEvent.AddListenerVerified(this, nameof(WorldUpdateAvailable))) {
                ErrorAndDisableGameObject($"Failed to listen to {nameof(UpdateAvailableEvent)}");
                return false;
            }

            if (!VersionConflictOccurredEvent.AddListenerVerified(this, nameof(VersionConflictOccurred))) {
                ErrorAndDisableGameObject($"Failed to listen to {nameof(VersionConflictOccurredEvent)}");
                return false;
            }

            if (NotificationTimeout <= 0) {
                Warn(
                        $"{nameof(NotificationTimeout)} <= 0; World version conflict notifications will be shown indefinitely");
            }

            return true;
        }

        public override void OnEvent(string eventName) {
            switch (eventName) {
                case nameof(WorldUpdateAvailable):
                    WorldUpdateAvailable();
                    return;
                case nameof(VersionConflictOccurred):
                    VersionConflictOccurred();
                    return;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }
        #endregion

        [PublicAPI]
        public void WorldUpdateAvailable() {
            Warn("World update available!");
            Canvas.gameObject.SetActive(true);

            UpdateAvailableText.gameObject.SetActive(true);

            ScheduleNotificationTimeout();
        }

        [PublicAPI]
        public void VersionConflictOccurred() {
            Warn("World versions differ. Udon Networking might break now.");
            Canvas.gameObject.SetActive(true);
            VersionConflictText.gameObject.SetActive(true);

            if (NotificationTimeout > 0) {
                ScheduleNotificationTimeout();
            }
        }


        [PublicAPI]
        public void HideNotifications() {
            if (NextUiHidingTime - Time.timeSinceLevelLoad > 0.1f * NotificationTimeout) {
                return;
            }

            Canvas.gameObject.SetActive(false);
            VersionConflictText.gameObject.SetActive(false);
            UpdateAvailableText.gameObject.SetActive(false);
        }

        #region Internal
        private void ScheduleNotificationTimeout() {
            NextUiHidingTime = Time.timeSinceLevelLoad + NotificationTimeout;
            SendCustomEventDelayedSeconds(nameof(HideNotifications), NotificationTimeout);
        }
        #endregion
    }
}