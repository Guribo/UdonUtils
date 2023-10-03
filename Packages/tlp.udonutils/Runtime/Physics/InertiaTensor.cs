using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Physics
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class InertiaTensor : UdonSharpBehaviour
    {
        [SerializeField]
        private Rigidbody body;

        public bool useCustomInertiaOnEnable;
        public Vector3 customInertiaTensor;
        public Vector3 customInertiaTensorRotation = Quaternion.identity.eulerAngles;

        public void OnEnable()
        {
            if (useCustomInertiaOnEnable)
            {
                SetTensor(customInertiaTensor, Quaternion.Euler(customInertiaTensorRotation));
            }

            LogTensor();
            enabled = false;
        }

        public void Start()
        {
            OnEnable();
        }

        public void SetTensor(Vector3 tensor, Quaternion rotation)
        {
            if (!Utilities.IsValid(body))
            {
                Debug.LogError("Invalid Rigidbody", gameObject);
                return;
            }

            body.inertiaTensor = tensor;
            body.inertiaTensorRotation = rotation;
        }

        private void LogTensor()
        {
            if (!Utilities.IsValid(body))
            {
                Debug.LogError("Invalid Rigidbody", gameObject);
                return;
            }

            Debug.Log($"[{GetType()}.LogTensor] {body.gameObject.name}.inertiaTensor = {body.inertiaTensor}");
            Debug.Log(
                $"[{GetType()}.LogTensor] {body.gameObject.name}.inertiaTensorRotation = {body.inertiaTensorRotation.eulerAngles}"
            );
        }
    }
}