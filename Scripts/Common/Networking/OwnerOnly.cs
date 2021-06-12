using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonUtils.Scripts.Common.Networking
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class OwnerOnly : UdonSharpBehaviour
    {
        public GameObject[] gameObjects;

        public void OnEnable()
        {
            ToggleEnabled(VRC.SDKBase.Networking.IsOwner(gameObject));
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            ToggleEnabled(VRC.SDKBase.Networking.IsOwner(gameObject));
        }

        private void ToggleEnabled(bool enable)
        {
            if (gameObjects == null)
            {
                return;
            }

            foreach (var go in gameObjects)
            {
                if (!Utilities.IsValid(go))
                {
                    continue;
                }

                if (go.activeSelf == enable)
                {
                    continue;
                }
                
                go.SetActive(enable);                
            }
        }
    }
}