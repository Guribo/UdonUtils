#if UNITY_EDITOR

using UnityEngine;

namespace TLP.UdonUtils.EditorOnly
{
    public class UsageNote : MonoBehaviour
    {
        [Multiline]
        public string Note;
    }
}

#endif