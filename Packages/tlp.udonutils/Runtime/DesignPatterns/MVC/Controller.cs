using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.DesignPatterns.MVC
{
    /// <summary>
    /// The controller in the MVC pattern is responsible for receiving user input and updating the model and view
    /// accordingly.
    /// It acts as a bridge between the model and the view, ensuring that any changes made to the model
    /// are reflected in the view.
    /// The controller also handles any business logic related to user input.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(Controller), ExecutionOrder)]
    public abstract class Controller : MvcBase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = Model.ExecutionOrder + 100;

        public bool IsControllerInitialized { get; private set; }
        protected Model Model { get; private set; }
        protected View View { get; private set; }

        #region PublicAPI
        [PublicAPI]
        public bool _Initialize(Model model, View view) {
#if TLP_DEBUG
            _DebugLog(nameof(_Initialize));
#endif
            if (!string.IsNullOrEmpty(CriticalError)) {
                _Error($"Can not initialize again due to previous critical error: '{CriticalError}'");
                return false;
            }

            if (IsControllerInitialized) {
                _Warn("Already initialized");
                return false;
            }

            if (!Utilities.IsValid(view)) {
                _Error($"{nameof(view)} invalid");
                return false;
            }

            if (!Utilities.IsValid(model)) {
                _Error($"{nameof(model)} invalid");
                return false;
            }

            if (!string.IsNullOrEmpty(view.CriticalError)) {
                _Error($"{nameof(view)} has critical error: '{view.CriticalError}'");
                return false;
            }

            if (view.IsViewInitialized) {
                _Error($"{nameof(view)} is already initialized");
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

            View = view;
            Model = model;

            // setting it to true to prevent attempts to re-initialize controllers that have
            // failed to initialize and are in need of cleanup
            IsControllerInitialized = true;

            if (_InitializeInternal()) {
                return true;
            }

            _Error($"Initialization failed. Using {nameof(_DeInitialize)} to cleanup.");
            _DeInitialize();
            return false;
        }

        protected virtual void OnDestroy() {
#if TLP_DEBUG
            _DebugLog(nameof(OnDestroy));
#endif
            _DeInitialize();
        }

        public bool _DeInitialize() {
#if TLP_DEBUG
            _DebugLog(nameof(_DeInitialize));
#endif
            if (!IsControllerInitialized) {
                return false;
            }

            if (Utilities.IsValid(View)) {
                View._DeInitialize();
            }

            View = null;

            if (Utilities.IsValid(Model)) {
                Model._DeInitialize();
            }

            Model = null;

            if (_DeInitializeInternal()) {
                IsControllerInitialized = false;
                CriticalError = null;
                return true;
            }

            CriticalError = $"De-Initialization failed.";
            _Error(CriticalError);
            return false;
        }
        #endregion
    }
}