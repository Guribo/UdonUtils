using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.DesignPatterns.MVC
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(View), ExecutionOrder)]
    public abstract class View : MvcBase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = Controller.ExecutionOrder + 100;

        public bool IsViewInitialized { get; private set; }
        public Model Model { get; private set; }
        public Controller Controller { get; private set; }
        private UdonEvent _modelChangeEvent;

        protected virtual void OnDestroy() {
#if TLP_DEBUG
            _DebugLog(nameof(OnDestroy));
#endif
            _DeInitialize();
        }

        #region PublicAPI
        [PublicAPI]
        public bool _Initialize(Controller optionalController, Model model) {
#if TLP_DEBUG
            _DebugLog(nameof(_Initialize));
#endif
            if (!string.IsNullOrEmpty(CriticalError)) {
                _Error($"Can not initialize again due to previous critical error: '{CriticalError}'");
                return false;
            }

            if (IsViewInitialized) {
                _Warn("Already initialized");
                return false;
            }

            if (!Utilities.IsValid(model)) {
                _Error($"{nameof(model)} invalid");
                return false;
            }

            if (!string.IsNullOrEmpty(model.CriticalError)) {
                _Error($"{nameof(model)} has critical error: '{model.CriticalError}'");
                return false;
            }

            if (!model.IsModelInitialized) {
                _Error($"{nameof(model)} is not initialized");
                return false;
            }

            if (Utilities.IsValid(optionalController)) {
                if (!string.IsNullOrEmpty(optionalController.CriticalError)) {
                    _Error($"{nameof(optionalController)} has critical error: '{optionalController.CriticalError}'");
                    return false;
                }

                if (!optionalController.IsControllerInitialized) {
                    _Error($"{nameof(optionalController)} is not initialized");
                    return false;
                }

                Controller = optionalController;
            } else {
                Controller = null;
            }

            if (!Utilities.IsValid(model.ChangeEvent)) {
                _Error($"{nameof(model.ChangeEvent)} invalid");
                return false;
            }

            Model = model;
            _modelChangeEvent = model.ChangeEvent;
            _modelChangeEvent._AddListenerVerified(this, nameof(_OnModelChanged));

            // setting it to true to prevent attempts to re-initialize controllers that have
            // failed to initialize and are in need of cleanup
            IsViewInitialized = true;
            if (_InitializeInternal()) {
                return true;
            }

            _Error($"Initialization failed. Using {nameof(_DeInitialize)} to cleanup.");
            _DeInitialize();
            return false;
        }

        public bool _DeInitialize() {
#if TLP_DEBUG
            _DebugLog(nameof(_DeInitialize));
#endif
            if (!IsViewInitialized) {
                return false;
            }

            if (Utilities.IsValid(_modelChangeEvent)) {
                _modelChangeEvent._RemoveListener(this, false);
                _modelChangeEvent = null;
            }

            Model = null;

            if (_DeInitializeInternal()) {
                IsViewInitialized = false;
                CriticalError = null;
                return true;
            }

            CriticalError = $"De-Initialization failed.";
            _Error(CriticalError);
            return false;
        }
        #endregion

        #region Hooks
        public abstract void _OnModelChanged();
        #endregion

        #region Internal
        public override void _OnEvent(string eventName) {
            switch (eventName) {
                case nameof(_OnModelChanged):
                    #region TLP_DEBUG
#if TLP_DEBUG
                    _DebugLog_OnEvent(eventName);
#endif
                    #endregion

                    _OnModelChanged();
                    break;
                default:
                    base._OnEvent(eventName);
                    return;
            }
        }
        #endregion
    }
}