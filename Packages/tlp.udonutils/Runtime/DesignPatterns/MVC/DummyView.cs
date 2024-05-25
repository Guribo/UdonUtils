using UdonSharp;

namespace TLP.UdonUtils.Runtime.DesignPatterns.MVC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DummyView : View
    {
        public override void OnModelChanged() {
        }
    }
}