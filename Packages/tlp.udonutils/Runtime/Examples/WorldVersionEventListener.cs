using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace TLP.UdonUtils.Examples
{
    /// <summary>
    /// Example of a UdonBehaviour that can get notified when a player with a different world version joined.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldVersionEventListener : TlpBaseBehaviour
    {
        public Canvas canvas;
        public Text updateAvailableText;
        public Text versionConflictText;

        public float notificationTimeout = 20f;

        internal float HidingUiTime;

        [PublicAPI]
        public void WorldUpdateAvailable()
        {
            DebugLog("Yay! Update available!");
            if (Utilities.IsValid(canvas))
            {
                canvas.gameObject.SetActive(true);
            }
            if (Utilities.IsValid(canvas))
            {
                updateAvailableText.gameObject.SetActive(true);
            }

            ScheduleNotificationTimeout();
        }

        [PublicAPI]
        public void VersionConflictOccurred()
        {
            DebugLog("Yay! UDON Networking might break now!");
            if (Utilities.IsValid(canvas))
            {
                canvas.gameObject.SetActive(true);
            }
            if (Utilities.IsValid(canvas))
            {
                versionConflictText.gameObject.SetActive(true);
            }

            ScheduleNotificationTimeout();
        }
        
        private void ScheduleNotificationTimeout()
        {
            HidingUiTime = Time.unscaledTime + notificationTimeout;
            SendCustomEventDelayedSeconds(nameof(HideNotifications), notificationTimeout);
        }

        [PublicAPI]
        public void HideNotifications()
        {
            if (HidingUiTime - Time.unscaledTime > 0.1f * notificationTimeout)
            {
                return;
            }
            
            if (Utilities.IsValid(canvas))
            {
                canvas.gameObject.SetActive(false);
            }
            if (Utilities.IsValid(canvas))
            {
                versionConflictText.gameObject.SetActive(false);
            }
            
            if (Utilities.IsValid(canvas))
            {
                updateAvailableText.gameObject.SetActive(false);
            }
        }
    }
}
