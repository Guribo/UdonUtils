using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.DesignPatterns.MVC
{
    [DefaultExecutionOrder(ExecutionOrder)]
    public abstract class View : MvcBase
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = Controller.ExecutionOrder + 1;

        public bool Initialized { get; private set; }
        public Model Model { get; private set; }
        public Controller Controller { get; private set; }
        private UdonEvent _modelChangeEvent;

        protected virtual void OnDestroy()
        {
#if TLP_DEBUG
            DebugLog(nameof(OnDestroy));
#endif
            DeInitialize();
        }

        #region PublicAPI

        [PublicAPI]
        public bool Initialize(Controller optionalController, Model model)
        {
#if TLP_DEBUG
            DebugLog(nameof(Initialize));
#endif
            if (HasError)
            {
                Error($"Can not initialize again due to previous critical error: '{CriticalError}'");
                return false;
            }

            if (Initialized)
            {
                Warn("Already initialized");
                return false;
            }

            if (!Utilities.IsValid(model))
            {
                Error($"{nameof(model)} invalid");
                return false;
            }

            if (model.HasError)
            {
                Error($"{nameof(model)} has critical error: '{model.CriticalError}'");
                return false;
            }

            if (!model.Initialized)
            {
                Error($"{nameof(model)} is not initialized");
                return false;
            }


            if (Utilities.IsValid(optionalController))
            {
                if (optionalController.HasError)
                {
                    Error($"{nameof(optionalController)} has critical error: '{optionalController.CriticalError}'");
                    return false;
                }

                if (!optionalController.Initialized)
                {
                    Error($"{nameof(optionalController)} is not initialized");
                    return false;
                }

                Controller = optionalController;
            }
            else
            {
                Controller = null;
            }

            if (!Utilities.IsValid(model.ChangeEvent))
            {
                Error($"{nameof(model.ChangeEvent)} invalid");
                return false;
            }

            Model = model;
            _modelChangeEvent = model.ChangeEvent;
            _modelChangeEvent.AddListenerVerified(this, nameof(OnModelChanged));

            // setting it to true to prevent attempts to re-initialize controllers that have
            // failed to initialize and are in need of cleanup
            Initialized = true;
            if (InitializeInternal())
            {
                return true;
            }

            Error($"Initialization failed. Using {nameof(DeInitialize)} to cleanup.");
            DeInitialize();
            return false;
        }

        public bool DeInitialize()
        {
#if TLP_DEBUG
            DebugLog(nameof(DeInitialize));
#endif
            if (!Initialized)
            {
                return false;
            }

            if (Utilities.IsValid(_modelChangeEvent))
            {
                _modelChangeEvent.RemoveListener(this, false);
                _modelChangeEvent = null;
            }

            Model = null;

            if (DeInitializeInternal())
            {
                Initialized = false;
                CriticalError = null;
                return true;
            }

            CriticalError = $"De-Initialization failed.";
            Error(CriticalError);
            HasError = true;
            return false;
        }

        #endregion

        #region Hooks

        public abstract void OnModelChanged();

        #endregion

        #region Internal

        public override void OnEvent(string eventName)
        {
            switch (eventName)
            {
                case nameof(OnModelChanged):
                    if (Initialized && !HasError)
                    {
                        OnModelChanged();
                    }
                    else
                    {
                        Warn($"Ignoring '{eventName}' as not initialized or error has occurred");
                        base.OnEvent(eventName);
                    }

                    break;
                default:
                    base.OnEvent(eventName);
                    return;
            }
        }

        #endregion
    }
}