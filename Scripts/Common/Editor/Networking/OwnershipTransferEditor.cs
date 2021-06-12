
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using Guribo.UdonUtils.Scripts.Common.Networking;
using UnityEditor;

namespace Guribo.UdonUtils.Scripts.Common.Editor.Networking
{
    [CustomEditor(typeof(OwnershipTransfer))]
    public class OwnershipTransferEditor : UdonLibraryEditor
    {
        protected override string GetSymbolName()
        {
            return "ownershipTransfer";
        }
    }
}
#endif

