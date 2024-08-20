using UdonSharp;

namespace TLP.UdonUtils.Runtime.DesignPatterns.MVC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DummyView : View
    {
        public override void OnModelChanged() {
        }
    }
}