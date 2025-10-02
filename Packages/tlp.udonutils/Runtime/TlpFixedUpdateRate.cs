using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using UdonSharp;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Tlp.UdonUtils.Runtime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TlpFixedUpdateRate), ExecutionOrder)]
    public class TlpFixedUpdateRate : TlpSingleton
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.Min;
        #endregion

        [SerializeField]
        private float InitialFixedUpdateRate = 64f;

        public float UpdateRate
        {
            set
            {
                _targetFixedUpdateRate = Mathf.Clamp(value, 8, 1024);
                _targetFixedDeltaTime = 1f / _targetFixedUpdateRate;
            }
            get => _targetFixedUpdateRate;
        }

        private float _targetFixedUpdateRate;
        private float _targetFixedDeltaTime = 1f / 60f;

        #region PublicAPI
        /// <summary>
        /// Searches for the GameObject TLP_FixedUpdateRate in the scene in order to get its TlpFixedUpdateRate component
        /// </summary>
        /// <returns>the found component or null if not found</returns>
        public static TlpFixedUpdateRate GetInstance() {
            var instance = GameObject.Find("TLP_FixedUpdateRate");
            if (instance) {
                return instance.GetComponentInChildren<TlpFixedUpdateRate>(true);
            }

            Debug.LogError("GameObject called 'TLP_FixedUpdateRate' not found");
            return null;
        }
        #endregion

#if TLP_DEBUG
        private void FixedUpdate() {
            if (Time.frameCount % 100 == 0) {
                Info(
                        $"{nameof(FixedUpdate)}: Current fixed delta time={Time.fixedDeltaTime:F6}s, target: {_targetFixedDeltaTime:F6}s ({_targetFixedUpdateRate:F3}/s)");
            }
        }

        public override void PostLateUpdate() {
            if (Time.frameCount % 100 == 0) {
                Info(
                        $"{nameof(PostLateUpdate)}: Current fixed delta time={Time.fixedDeltaTime:F6}s, target: {_targetFixedDeltaTime:F6}s ({_targetFixedUpdateRate:F3}/s)");
            }
        }
#endif

        private void Update() {
#if TLP_DEBUG
            if (Time.frameCount % 100 == 0) {
                Info(
                        $"{nameof(Update)}: Current fixed delta time={Time.fixedDeltaTime:F6}s, target: {_targetFixedDeltaTime:F6}s ({_targetFixedUpdateRate:F3}/s)");
            }

            if (Input.GetKey(KeyCode.U)) {
                UpdateRate++;
            } else if (
                    Input.GetKey(KeyCode.J)) {
                UpdateRate--;
            }
#endif
            Time.fixedDeltaTime = _targetFixedDeltaTime;
        }

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            UpdateRate = InitialFixedUpdateRate;
            return true;
        }
    }
}