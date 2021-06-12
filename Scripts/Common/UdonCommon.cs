using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonUtils.Scripts.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UdonCommon : UdonSharpBehaviour
    {
        /// <summary>
        /// Finds the component of a given type in the current gameobject hierarchy which is closest to the scene root
        /// </summary>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <returns>the found component or null if none was found</returns>
        public Component FindTopComponent(Type type, Transform start)
        {
            if (!Utilities.IsValid(start))
            {
                return null;
            }

            Component topComponent = null;
            var topTransform = start;

            do
            {
                var behaviour = topTransform.GetComponent(type);
                if (Utilities.IsValid(behaviour))
                {
                    topComponent = behaviour;
                }

                topTransform = topTransform.parent;
            } while (Utilities.IsValid(topTransform));

            return topComponent;
        }
    }
}
