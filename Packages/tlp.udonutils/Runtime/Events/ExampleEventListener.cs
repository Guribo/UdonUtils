using System;
using UnityEngine;

namespace TLP.UdonUtils.Events
{
    public class ExampleEventListener : TlpBaseBehaviour
    {
        [SerializeField]
        private UdonEvent UdonEvent;

        private void Start() {
            if (!UdonEvent.AddListenerVerified(this, nameof(UdonEventFunctionName))) {
                Error($"Failed to listen to {nameof(UdonEvent)}");
            }
        }

        public override void OnEvent(string eventName) {
            switch (eventName) {
                case nameof(UdonEventFunctionName):
                    UdonEventFunctionName();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }

        private void UdonEventFunctionName() {
            // your code here, will be called whenever UdonEvent fires
        }
    }
}