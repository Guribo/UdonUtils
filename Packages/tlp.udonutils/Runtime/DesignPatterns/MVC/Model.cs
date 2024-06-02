using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.DesignPatterns.MVC
{
    [DefaultExecutionOrder(ExecutionOrder)]
    public abstract class Model : MvcBase
    {
        public new const int ExecutionOrder = TlpExecutionOrder.UiStart;
        private const string OnModelChangedCallbackName = "OnModelChanged";

        public bool Initialized { get; private set; }

        public UdonEvent ChangeEvent { get; private set; }


        #region PublicAPI
        public bool IsReady() {
            if (HasError) {
                Error("Can not add due to previous critical Error");
                return false;
            }

            if (Initialized) {
                return true;
            }

            Error("Not initialized");
            return false;
        }

        [PublicAPI]
        public bool Initialize(UdonEvent changeEvent) {
#if TLP_DEBUG
            DebugLog($"{nameof(Initialize)} with '{changeEvent.GetScriptPathInScene()}");
#endif
            if (HasError) {
                Error($"Can not initialize again due to previous critical error: '{CriticalError}'");
                return false;
            }

            if (Initialized) {
                Error("Already initialized");
                return false;
            }

            if (!Utilities.IsValid(changeEvent)) {
                Error($"{nameof(changeEvent)} invalid");
                return false;
            }

            Dirty = false;
            ChangeEvent = changeEvent;
            ChangeEvent.ListenerMethod = OnModelChangedCallbackName;
            if (!ChangeEvent.AddListenerVerified(this, OnModelChangedCallbackName)) {
                Error($"Adding to {nameof(ChangeEvent)} with callback '{OnModelChangedCallbackName}' failed");
                return false;
            }

            // setting it to true to prevent attempts to re-initialize controllers that have
            // failed to initialize and are in need of cleanup
            Initialized = true;

            if (InitializeInternal()) {
                return true;
            }

            Error($"Initialization failed. Using {nameof(DeInitialize)} to cleanup.");
            DeInitialize();
            return false;
        }

        public override void OnEvent(string eventName) {
#if TLP_DEBUG
            DebugLog($"{nameof(OnEvent)} {eventName}");
#endif

            if (eventName == OnModelChangedCallbackName && ReferenceEquals(EventInstigator, this)) {
                Dirty = false;
                return;
            }

            base.OnEvent(eventName);
        }


        /// <summary>
        /// Raises the event instantly if dirty and delayFrames == 0.
        /// No-Op if not dirty.
        /// Delays the event if dirty and delayFrames > 0 by delayFrames frames.
        /// </summary>
        /// <param name="delayFrames"></param>
        /// <returns>true if not dirty or the event was successfully raised or scheduled, false only on error, note that in case of delayFrames > 0 there might be errors at a later point in time that are not captured here</returns>
        public bool NotifyIfDirty(int delayFrames = 0) {
            if (!Dirty) {
                return true;
            }

            if (HasError || !Initialized) {
                Error(HasError ? "Has Error" : "Not Initialized");
                return false;
            }

            if (!Utilities.IsValid(ChangeEvent)) {
                Error($"{nameof(ChangeEvent)} invalid");
                return false;
            }

            if (delayFrames < 1) {
                if (!ChangeEvent.Raise(this)) {
                    Error($"Failed to raise {nameof(ChangeEvent)} '{ChangeEvent.ListenerMethod}'");
                    return false;
                }

                return true;
            }

            if (!ChangeEvent.RaiseOnIdle(this, delayFrames)) {
                Error($"Failed to raise {nameof(ChangeEvent)} '{ChangeEvent.ListenerMethod}'");
                return false;
            }

            return true;
        }

        public virtual bool Dirty { get; set; }

        protected virtual void OnDestroy() {
#if TLP_DEBUG
            DebugLog(nameof(OnDestroy));
#endif
            DeInitialize();
        }

        public bool DeInitialize() {
#if TLP_DEBUG
            DebugLog(nameof(DeInitialize));
#endif
            if (!Initialized) {
                return false;
            }

            if (Utilities.IsValid(ChangeEvent)) {
                if (!ChangeEvent.RemoveListener(this, true)) {
                    Warn($"{nameof(ChangeEvent)} wasn't being listened to");
                }
            }

            if (DeInitializeInternal()) {
                ChangeEvent = null;
                Initialized = false;
                CriticalError = null;
                return true;
            }

            ChangeEvent = null;
            CriticalError = $"De-Initialization failed.";
            Error(CriticalError);
            HasError = true;
            return false;
        }
        #endregion
    }
}