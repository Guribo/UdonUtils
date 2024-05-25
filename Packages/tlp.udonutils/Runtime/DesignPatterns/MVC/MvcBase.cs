using TLP.UdonUtils.Runtime.Events;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.DesignPatterns.MVC
{
    public abstract class MvcBase : TlpBaseBehaviour
    {
        public string CriticalError { get; protected set; }

        public bool HasError { get; protected set; }

        public static bool InitializeMvcSingleGameObject(GameObject gameObject) {
            return InitializeMvc(
                    gameObject.GetComponent<Model>(),
                    gameObject.GetComponent<View>(),
                    gameObject.GetComponent<Controller>(),
                    gameObject.GetComponent<UdonEvent>()
            );
        }

        public static bool InitializeMvc(
                Model model,
                View view,
                Controller controller,
                UdonEvent modelChangedEvent
        ) {
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