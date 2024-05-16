using TLP.UdonUtils.DesignPatterns.MVC;
using UdonSharp;
using VRC.SDK3.Data;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TestData : Model
{
    public DataList Tests = new DataList();
}