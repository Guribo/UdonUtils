using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.DesignPatterns.MVC
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(Model), ExecutionOrder)]
    public abstract class Model : MvcBase
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = MvcBase.ExecutionOrder + 1;
        #endregion

        #region Constants
        private const string OnModelChangedCallbackName = "OnModelChanged";
        #endregion

        #region State
        /// <summary>
        /// True if Model is currently
        /// initialized with <see cref="Initialize"/>.
        /// </summary>
        public bool IsModelInitialized { get; private set; }

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

            if (!IsReceivingStart) {
                if (!HasStartedOk) {
                    Error($"{nameof(Initialize)}: {nameof(Initialize)}: Not initialized");
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(CriticalError)) {
                Error($"{nameof(Initialize)}: Can not initialize again due to previous critical error: '{CriticalError}'");
                return false;
            }

            if (IsModelInitialized) {
                Error($"{nameof(Initialize)}: Already initialized");
                return false;
            }

            if (!Utilities.IsValid(changeEvent)) {
                Error($"{nameof(Initialize)}: {nameof(changeEvent)} invalid");
                return false;
            }

            Dirty = false;
            ChangeEvent = changeEvent;
            ChangeEvent.ListenerMethod = OnModelChangedCallbackName;
            if (!ChangeEvent.AddListenerVerified(this, OnModelChangedCallbackName)) {
                Error($"{nameof(Initialize)}: Adding to {nameof(ChangeEvent)} with callback '{OnModelChangedCallbackName}' failed");
                return false;
            }

            // setting it to true to prevent attempts to re-initialize controllers that have
            // failed to initialize and are in need of cleanup
            IsModelInitialized = true;

            if (InitializeInternal()) {
                return true;
            }

            Error($"{nameof(Initialize)}: Initialization failed. Using {nameof(DeInitialize)} to cleanup.");
            DeInitialize();
            return false;
        }

        public bool DeInitialize() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(DeInitialize));
#endif
            #endregion

            if (!IsModelInitialized) {
                return false;
            }

            if (Utilities.IsValid(ChangeEvent)) {
                if (!ChangeEvent.RemoveListener(this, true)) {
                    Warn($"{nameof(DeInitialize)}: {nameof(ChangeEvent)} wasn't being listened to");
                }
            }

            if (DeInitializeInternal()) {
                ChangeEvent = null;
                IsModelInitialized = false;
                CriticalError = null;
                return true;
            }

            ChangeEvent = null;
            CriticalError = $"De-Initialization failed.";
            Error(CriticalError);
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

            if (!HasStartedOk) {
                Error($"{nameof(NotifyIfDirty)}: Not initialized");
                return false;
            }

            if (!Dirty) {
                return true;
            }

            if (!Utilities.IsValid(ChangeEvent)) {
                Error($"{nameof(NotifyIfDirty)}: {nameof(ChangeEvent)} invalid");
                return false;
            }

            if (delayFrames < 1) {
                if (!ChangeEvent.Raise(this)) {
                    Error($"{nameof(NotifyIfDirty)}: Failed to raise {nameof(ChangeEvent)} '{ChangeEvent.ListenerMethod}'");
                    return false;
                }

                return true;
            }

            if (!ChangeEvent.RaiseOnIdle(this, delayFrames)) {
                Error($"{nameof(NotifyIfDirty)}: Failed to raise {nameof(ChangeEvent)} '{ChangeEvent.ListenerMethod}'");
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