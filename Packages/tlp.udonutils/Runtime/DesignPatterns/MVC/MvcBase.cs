using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Player;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.DesignPatterns.MVC
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(MvcBase), ExecutionOrder)]
    public abstract class MvcBase : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerFollowerUi.ExecutionOrder + 100;

        public string CriticalError { get; protected set; }

        public bool HasError { get; protected set; }

        public static bool InitializeMvcSingleGameObject(GameObject gameObject) {
            if (!Utilities.IsValid(gameObject)) {
                Debug.LogError($"{nameof(InitializeMvcSingleGameObject)}: {nameof(gameObject)} invalid");
                return false;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            Debug.Log($"{nameof(InitializeMvcSingleGameObject)} '{gameObject.transform.GetPathInScene()}'");
#endif
            #endregion

            if (InitializeMvc(
                        gameObject.GetComponent<Model>(),
                        gameObject.GetComponent<View>(),
                        gameObject.GetComponent<Controller>(),
                        gameObject.GetComponent<UdonEvent>())) {
                return true;
            }

            Debug.LogError($"Failed to Initialize MVC on '{gameObject.transform.GetPathInScene()}'", gameObject);
            return false;
        }

        public static bool InitializeMvc(
                Model model,
                View view,
                Controller controller,
                UdonEvent modelChangedEvent
        ) {
            #region TLP_DEBUG
#if TLP_DEBUG
            Debug.Log(nameof(InitializeMvc));
#endif
            #endregion

            if (!Utilities.IsValid(model)) {
                Debug.LogError($"{nameof(model)} invalid");
                return false;
            }

            if (!Utilities.IsValid(view)) {
                Debug.LogError($"{nameof(view)} invalid");
                return false;
            }

            if (!Utilities.IsValid(controller)) {
                Debug.LogError($"{nameof(controller)} invalid");
                return false;
            }

            if (!Utilities.IsValid(modelChangedEvent)) {
                Debug.LogError($"{nameof(modelChangedEvent)} invalid");
                return false;
            }

            return model.Initialize(modelChangedEvent)
                   && controller.Initialize(model, view)
                   && view.Initialize(controller, model);
        }

        #region Hooks
        protected virtual bool InitializeInternal() {
            return true;
        }

        protected virtual bool DeInitializeInternal() {
            return true;
        }
        #endregion
    }
}