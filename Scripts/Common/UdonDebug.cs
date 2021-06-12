using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace Guribo.UdonUtils.Scripts.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UdonDebug : UdonSharpBehaviour
    {
        public bool Assert(bool condition, string message, Object context)
        {
            if (!condition)
            {
                if (Utilities.IsValid(context))
                {
                    Debug.LogError("Assertion failed : '" + context.GetType() + " : " + message + "'", context);
                }
                else
                {
                    Debug.LogError("Assertion failed :  'UNKNOWN TYPE: " + message + "'");
                }

                return false;
            }

            Debug.Assert(condition, message);
            return true;
        }
    }
}
