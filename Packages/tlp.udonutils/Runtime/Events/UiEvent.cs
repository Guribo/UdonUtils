using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Events
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UiEvent : UdonEvent
    {
    }
}