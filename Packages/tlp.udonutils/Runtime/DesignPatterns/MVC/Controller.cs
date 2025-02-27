﻿using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
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
        public bool Initialize(Model model, View view) {
#if TLP_DEBUG
            DebugLog(nameof(Initialize));
#endif
            if (!string.IsNullOrEmpty(CriticalError)) {
                Error($"Can not initialize again due to previous critical error: '{CriticalError}'");
                return false;
            }

            if (IsControllerInitialized) {
                Warn("Already initialized");
                return false;
            }

            if (!Utilities.IsValid(view)) {
                Error($"{nameof(view)} invalid");
                return false;
            }

            if (!Utilities.IsValid(model)) {
                Error($"{nameof(model)} invalid");
                return false;
            }

            if (!string.IsNullOrEmpty(view.CriticalError)) {
                Error($"{nameof(view)} has critical error: '{view.CriticalError}'");
                return false;
            }

            if (view.IsViewInitialized) {
                Error($"{nameof(view)} is already initialized");
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

            View = view;
            Model = model;

            // setting it to true to prevent attempts to re-initialize controllers that have
            // failed to initialize and are in need of cleanup
            IsControllerInitialized = true;

            if (InitializeInternal()) {
                return true;
            }

            Error($"Initialization failed. Using {nameof(DeInitialize)} to cleanup.");
            DeInitialize();
            return false;
        }

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
            if (!IsControllerInitialized) {
                return false;
            }

            if (Utilities.IsValid(View)) {
                View.DeInitialize();
            }

            View = null;

            if (Utilities.IsValid(Model)) {
                Model.DeInitialize();
            }

            Model = null;

            if (DeInitializeInternal()) {
                IsControllerInitialized = false;
                CriticalError = null;
                return true;
            }

            CriticalError = $"De-Initialization failed.";
            Error(CriticalError);
            return false;
        }
        #endregion
    }
}