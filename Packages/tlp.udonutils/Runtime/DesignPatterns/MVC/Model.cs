using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
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
        public const string OnModelChangedCallbackName = "_OnModelChanged";
        #endregion

        #region State
        /// <summary>
        /// True if Model is currently
        /// initialized with <see cref="_Initialize"/>.
        /// </summary>
        public bool IsModelInitialized { get; private set; }

        public virtual bool Dirty
        {
            get { return _dirty; }
            set
            {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"{nameof(Dirty)}: {nameof(_dirty)}={value}");
#endif
                #endregion


                _dirty = value;
            }
        }

        private bool _dirty;

        public UdonEvent ChangeEvent { get; private set; }
        #endregion

        #region PublicAPI
        [PublicAPI]
        public bool _Initialize(UdonEvent changeEvent) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_Initialize)} with '{changeEvent._GetScriptPathInScene()}'");
#endif
            #endregion

            if (!IsReceivingStart) {
                if (!HasStartedOk) {
                    _Error($"{nameof(_Initialize)}: {nameof(_Initialize)}: Not initialized");
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(CriticalError)) {
                _Error($"{nameof(_Initialize)}: Can not initialize again due to previous critical error: " +
                       $"'{CriticalError}'");
                return false;
            }

            if (IsModelInitialized) {
                _Error($"{nameof(_Initialize)}: Already initialized");
                return false;
            }

            if (!Utilities.IsValid(changeEvent)) {
                _Error($"{nameof(_Initialize)}: {nameof(changeEvent)} invalid");
                return false;
            }

            Dirty = false;
            ChangeEvent = changeEvent;
            ChangeEvent.ListenerMethod = OnModelChangedCallbackName;
            if (!ChangeEvent._AddListenerVerified(this, OnModelChangedCallbackName)) {
                _Error($"{nameof(_Initialize)}: Adding to {nameof(ChangeEvent)} with callback " +
                       $"'{OnModelChangedCallbackName}' failed");
                return false;
            }

            // setting it to true to prevent attempts to re-initialize controllers that have
            // failed to initialize and are in need of cleanup
            IsModelInitialized = true;

            if (_InitializeInternal()) {
                return true;
            }

            _Error($"{nameof(_Initialize)}: Initialization failed. Using {nameof(_DeInitialize)} to cleanup.");
            _DeInitialize();
            return false;
        }

        public bool _DeInitialize() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_DeInitialize));
#endif
            #endregion

            if (!IsModelInitialized) {
                return false;
            }

            if (Utilities.IsValid(ChangeEvent)) {
                if (!ChangeEvent._RemoveListener(this, true)) {
                    _Warn($"{nameof(_DeInitialize)}: {nameof(ChangeEvent)} wasn't being listened to");
                }
            }

            if (_DeInitializeInternal()) {
                ChangeEvent = null;
                IsModelInitialized = false;
                CriticalError = null;
                return true;
            }

            ChangeEvent = null;
            CriticalError = $"De-Initialization failed.";
            _Error(CriticalError);
            return false;
        }

        /// <summary>
        /// Raises the event instantly if dirty and delayFrames == 0.
        /// No-Op if not dirty.
        /// Delays the event if dirty and delayFrames > 0 by delayFrames frames.
        /// </summary>
        /// <param name="delayFrames"></param>
        /// <returns>true if not dirty or the event was successfully raised or scheduled, false only on error, note that in case of delayFrames > 0 there might be errors at a later point in time that are not captured here</returns>
        public bool _NotifyIfDirty(int delayFrames = 0) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_NotifyIfDirty)} delayFrames = {delayFrames}");
#endif
            #endregion

            if (!Dirty) {
                return true;
            }

            if (!IsReceivingStart && !HasStartedOk) {
                _Error($"{nameof(_NotifyIfDirty)}: Not initialized");
                return false;
            }

            if (!IsModelInitialized) {
                _Error($"{nameof(_NotifyIfDirty)}: Model not initialized");
                return false;
            }

            if (!Utilities.IsValid(ChangeEvent)) {
                _Error($"{nameof(_NotifyIfDirty)}: {nameof(ChangeEvent)} invalid");
                return false;
            }

            if (delayFrames < 1) {
                if (!ChangeEvent._Raise(this)) {
                    _Error(
                            $"{nameof(_NotifyIfDirty)}: Failed to raise {nameof(ChangeEvent)} '{ChangeEvent.ListenerMethod}'");
                    return false;
                }

                return true;
            }

            if (!ChangeEvent._RaiseOnIdle(this, delayFrames)) {
                _Error(
                        $"{nameof(_NotifyIfDirty)}: Failed to raise {nameof(ChangeEvent)} '{ChangeEvent.ListenerMethod}'");
                return false;
            }

            return true;
        }
        #endregion

        #region Overrides
        public override void _OnEvent(string eventName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_OnEvent)} {eventName}");
#endif
            #endregion

            if (eventName == OnModelChangedCallbackName && ReferenceEquals(EventInstigator, this)) {
                Dirty = false;
                return;
            }

            base._OnEvent(eventName);
        }
        #endregion

        #region Internal
        protected virtual void OnDestroy() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDestroy));
#endif
            #endregion

            _DeInitialize();
        }
        #endregion
    }
}