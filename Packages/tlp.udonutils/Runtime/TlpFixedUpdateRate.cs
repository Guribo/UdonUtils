using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using UdonSharp;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Tlp.UdonUtils.Runtime
{
    /// <summary>
    /// Overrides VRChat's <see cref="Time.fixedDeltaTime"/>.
    /// 
    /// This component continuously applies the target fixed delta time each frame, using the
    /// configured physics/update tick rate. Intended to be used as a singleton via a scene GameObject named
    /// <c>TLP_FixedUpdateRate</c>.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TlpFixedUpdateRate), ExecutionOrder)]
    public class TlpFixedUpdateRate : TlpSingleton
    {
        #region ExecutionOrder
        /// <summary>
        /// <inheritdoc cref="ExecutionOrder"/>
        /// </summary>
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        /// <summary>
        /// Run as early as VRChat permits to ensure the override is valid for the entire frame, including VRChat's
        /// own scripts like player movement etc.
        /// </summary>
        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.Min;
        #endregion

        /// <summary>
        /// Initial target fixed update rate (in ticks/second) applied during setup.
        /// </summary>
        [Tooltip("Initial target fixed update rate (in ticks/second) applied during setup.")]
        [SerializeField]
        private float InitialFixedUpdateRate = 64f;

        /// <summary>
        /// Gets or sets the target fixed update rate (in ticks/second).
        /// 
        /// Setting this value clamps it to a safe range and updates the internally cached target fixed delta time
        /// (<c>1 / rate</c>), which is then applied to <see cref="Time.fixedDeltaTime"/>.
        /// </summary>
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
        /// Finds and returns the active <see cref="TlpFixedUpdateRate"/> instance from a scene GameObject named
        /// <c>TLP_FixedUpdateRate</c>.
        /// </summary>
        /// <returns>
        /// The located <see cref="TlpFixedUpdateRate"/> component, or <c>null</c> if the GameObject cannot be found.
        /// </returns>
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