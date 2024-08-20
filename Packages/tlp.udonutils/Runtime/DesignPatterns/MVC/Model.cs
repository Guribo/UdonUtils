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
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.UiStart;
        #endregion

        #region Constants
        private const string OnModelChangedCallbackName = "OnModelChanged";
        #endregion

        #region State
        public bool Initialized { get; private set; }
        public virtual bool Dirty { get; set; }
        public UdonEvent ChangeEvent { get; private set; }
        #endregion

        #region PublicAPI
        [PublicAPI]
        public bool Initialize(UdonEvent changeEvent) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(Initialize)} with '{changeEvent.GetScriptPathInScene()}'");
#endif
            #endregion

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

        public bool IsReady() {
            if (HasError) {
                Error($"Not ready, has critical error: {CriticalError}");
                return false;
            }

            if (Initialized) {
                return true;
            }

            Error("Not initialized");
            return false;
        }

        public bool DeInitialize() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(DeInitialize));
#endif
            #endregion

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

        /// <summary>
        /// Raises the event instantly if dirty and delayFrames == 0.
        /// No-Op if not dirty.
        /// Delays the event if dirty and delayFrames > 0 by delayFrames frames.
        /// </summary>
        /// <param name="delayFrames"></param>
        /// <returns>true if not dirty or the event was successfully raised or scheduled, false only on error, note that in case of delayFrames > 0 there might be errors at a later point in time that are not captured here</returns>
        public bool NotifyIfDirty(int delayFrames = 0) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(NotifyIfDirty)} delayFrames = {delayFrames}");
#endif
            #endregion

            if (!Dirty) {
                return true;
            }

            if (!IsReady()) {
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
        #endregion

        #region Overrides
        public override void OnEvent(string eventName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnEvent)} {eventName}");
#endif
            #endregion

            if (eventName == OnModelChangedCallbackName && ReferenceEquals(EventInstigator, this)) {
                Dirty = false;
                return;
            }

            base.OnEvent(eventName);
        }
        #endregion

        #region Internal
        protected virtual void OnDestroy() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDestroy));
#endif
            #endregion

            DeInitialize();
        }
        #endregion
    }
}