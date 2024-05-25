#if UNITY_EDITOR

using UnityEngine;

namespace TLP.UdonUtils.Runtime.EditorOnly
{
    public class UsageNote : MonoBehaviour
    {
        [Multiline]
        public string Note;
    }
}

#endif