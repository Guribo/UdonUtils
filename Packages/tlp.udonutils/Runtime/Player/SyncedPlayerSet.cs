using UdonSharp;

namespace TLP.UdonUtils.Runtime.Player
{
    /// <summary>
    /// Version of the <see cref="PlayerSet"/> that uses no networking
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncedPlayerSet : PlayerSet
    {
        // no changes
    }
}