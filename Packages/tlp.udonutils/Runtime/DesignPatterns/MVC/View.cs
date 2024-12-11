using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using UnityEngine;
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
            DebugLog(nameof(OnDestroy));
#endif
            DeInitialize();
        }

        #region PublicAPI
        [PublicAPI]
        public bool Initialize(Controller optionalController, Model model) {
#if TLP_DEBUG
            DebugLog(nameof(Initialize));
#endif
            if (!string.IsNullOrEmpty(CriticalError)) {
                Error($"Can not initialize again due to previous critical error: '{CriticalError}'");
                return false;
            }

            if (IsViewInitialized) {
                Warn("Already initialized");
                return false;
            }

            if (!Utilities.IsValid(model)) {
                Error($"{nameof(model)} invalid");
                return false;
            }

            if (!string.IsNullOrEmpty(model.CriticalError)) {
                Error($"{nameof(model)} has critical error: '{model.CriticalError}'");
                return false;
            }

            if (!model.IsModelInitialized) {
                Error($"{nameof(model)} is not initialized");
                return false;
            }

            if (Utilities.IsValid(optionalController)) {
                if (!string.IsNullOrEmpty(optionalController.CriticalError)) {
                    Error($"{nameof(optionalController)} has critical error: '{optionalController.CriticalError}'");
                    return false;
                }

                if (!optionalController.IsControllerInitialized) {
                    Error($"{nameof(optionalController)} is not initialized");
                    return false;
                }

                Controller = optionalController;
            } else {
                Controller = null;
            }

            if (!Utilities.IsValid(model.ChangeEvent)) {
                Error($"{nameof(model.ChangeEvent)} invalid");
                return false;
            }

            Model = model;
            _modelChangeEvent = model.ChangeEvent;
            _modelChangeEvent.AddListenerVerified(this, nameof(OnModelChanged));

            // setting it to true to prevent attempts to re-initialize controllers that have
            // failed to initialize and are in need of cleanup
            IsViewInitialized = true;
            if (InitializeInternal()) {
                return true;
            }

            Error($"Initialization failed. Using {nameof(DeInitialize)} to cleanup.");
            DeInitialize();
            return false;
        }

        public bool DeInitialize() {
#if TLP_DEBUG
            DebugLog(nameof(DeInitialize));
#endif
            if (!IsViewInitialized) {
                return false;
            }

            if (Utilities.IsValid(_modelChangeEvent)) {
                _modelChangeEvent.RemoveListener(this, false);
                _modelChangeEvent = null;
            }

            Model = null;

            if (DeInitializeInternal()) {
                IsViewInitialized = false;
                CriticalError = null;
                return true;
            }

            CriticalError = $"De-Initialization failed.";
            Error(CriticalError);
            return false;
        }
        #endregion

        #region Hooks
        public abstract void OnModelChanged();
        #endregion

        #region Internal
        public override void OnEvent(string eventName) {
            switch (eventName) {
                case nameof(OnModelChanged):
                    #region TLP_DEBUG
#if TLP_DEBUG
        DebugLog_OnEvent(eventName);
#endif
                    #endregion

                    OnModelChanged();
                    break;
                default:
                    base.OnEvent(eventName);
                    return;
            }
        }
        #endregion
    }
}