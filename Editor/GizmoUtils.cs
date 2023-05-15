#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEngine;

namespace TLP.UdonUtils.Editor
{
    public class GizmoUtils : MonoBehaviour
    {
        public static void DrawArrow(Vector3 position, Vector3 direction, float length)
        {
            if (Mathf.Approximately(length, 0f))
            {
                return;
            }

            var normalizedDirection = direction.normalized;

            var rotation = Quaternion.LookRotation(normalizedDirection);
            DrawArrow(position, rotation, length);
        }

        public static void DrawArrow(Vector3 position, Quaternion rotation, float length)
        {
            if (Mathf.Approximately(length, 0f))
            {
                return;
            }

            var arrowHead = position + (rotation * Vector3.forward) * length;
            
            // arrow root line
            Gizmos.DrawLine(position, arrowHead);

            // arrow head with 4 lines
            var back = rotation * Vector3.back;
            var up = rotation * Vector3.up;
            var right = rotation * Vector3.right;

            var halfLength = length * 0.25f;

            Gizmos.DrawLine(arrowHead, arrowHead + (back + up) * halfLength);
            Gizmos.DrawLine(arrowHead, arrowHead + (back - up) * halfLength);
            Gizmos.DrawLine(arrowHead, arrowHead + (back + right) * halfLength);
            Gizmos.DrawLine(arrowHead, arrowHead + (back - right) * halfLength);
        }
    }
}
#endif
