using UnityEngine;

namespace TLP.UdonUtils.Runtime.Sync
{
    public class TransformSnapshot : TimeSnapshot
    {
        public Quaternion Rotation;
        public Vector3 Position;
    }
}