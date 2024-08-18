using System;
using System.Diagnostics;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon.Common;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TLP.UdonUtils.Runtime
{
    /// <summary>
    /// UdonSharpBehaviour Lifecycle<br/>
    ///
    /// 1. OnEnable<br/>
    /// 2. Start<br/>
    /// 3. OnDeserialization, OnOwnershipTransferred etc. (Networking Events)<br/>
    /// 4. FixedUpdate<br/>
    /// 5. AnimationEvents<br/>
    /// 6. CollisionEvents (OnPlayer[...], OnTrigger[...], OnCollision[...])<br/>
    /// 7. InputEvents (InPickup, InputJump, etc.)<br/>
    /// 8. Update<br/>
    /// 9. AnimationEvents<br/>
    /// 10. LateUpdate<br/>
    /// 11. PostLateUpdate (most accurate player position)<br/>
    /// 12. RenderingEvents (OnBecameVisible, OnRenderImage, etc.)<br/>
    /// 13. OnDisable<br/>
    /// 14. OnDestroy<br/>
    ///
    /// <remarks>https://docs.vrchat.com/docs/event-execution-order</remarks>
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    public abstract class TlpBaseBehaviour : UdonSharpBehaviour
    {
        protected virtual int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public const int ExecutionOrder = TlpExecutionOrder.DefaultStart;

        #region Constants
        protected const int False = 0;
        protected const int True = 1;

        protected const int InvalidPlayer = -1;
        protected const int NoUser = InvalidPlayer;
        private const string PlayerTagTlpLoggerMissingLogged = "TlpLoggerMissingLogged";
        #endregion

        #region Settings
        [Header("TLP/Networking")]
        /// <summary>
        /// If true and a serialization request fails it will automatically try
        /// to send again in the next frame until it succeeds.
        /// Only applies to owned objects and manual sync.
        /// </summary>
        [Tooltip(
                "If true and a serialization request fails it will automatically try " +
                "to send again in the next frame until it succeeds. " +
                "Only applies to owned objects and manual sync.")]
        public bool AutoRetrySendOnFailure = true;

        [Header("TLP/Logging")]
        /// <summary>
        /// What kind of logs of this behavior shall be produced.
        /// Selected severity includes all more severe levels.
        /// Example: selecting 'Warning' also allows 'Error' and 'Assertion' messages to appear.
        /// Note: 'Debug' messages are filtered out by default, even when selected.
        /// Add the compiler definition 'TLP_DEBUG' in the Unity player settings to enable them.
        /// This should only ever be used for debugging (performance suffers)!
        /// </summary>
        [FormerlySerializedAs("severity")]
        [Tooltip(
                "What kind of logs of this behavior shall be produced. " +
                "Selected severity includes all more severe levels. " +
                "Example: selecting 'Warning' also allows 'Error' and 'Assertion' messages to appear.\n" +
                "Note:\n'Debug' messages are filtered out by default, even when selected. " +
                "Add the compiler definition 'TLP_DEBUG' in the Unity player settings to enable them. " +
                "This should only ever be used for debugging (performance suffers)!")]
        public ELogLevel Severity = ELogLevel.Debug;
        #endregion

        #region Networking
        [PublicAPI]
        public bool IsPendingSerialization() {
            return PendingSerializations > 0;
        }

        [PublicAPI]
        public bool DropPendingSerializations() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(DropPendingSerializations));
#endif
            #endregion

            bool hadPending = IsPendingSerialization();
            PendingSerializations = 0;
            return hadPending;
        }

        internal int PendingSerializations;

        [PublicAPI]
        public bool MarkNetworkDirty() {
#if TLP_DEBUG
            DebugLog(nameof(MarkNetworkDirty));
#endif
            if (!Networking.IsOwner(gameObject)) {
                Warn("Can not mark network dirty, not owner");
                return false;
            }

            PendingSerializations = Math.Max(1, PendingSerializations + 1);
            SendCustomEventDelayedFrames(nameof(ExecuteScheduledSerialization), 0);
            return true;
        }

        public void ExecuteScheduledSerialization() {
#if TLP_DEBUG
            DebugLog(nameof(ExecuteScheduledSerialization));
#endif
            if (PendingSerializations < 1) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog("Nothing to do");
#endif
                #endregion

                return;
            }

            if (!Networking.IsOwner(gameObject)) {
                PendingSerializations = 0;

                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog("Not owner");
#endif
                #endregion

                return;
            }

            RequestSerialization();
        }

        public override void OnPreSerialization() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnPreSerialization));
#endif
            #endregion

            if (PendingSerializations < 1) {
                PendingSerializations = 1;
            }
        }

        public override void OnPostSerialization(SerializationResult result) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(
                    $"{nameof(OnPostSerialization)}: {(result.success ? $"Sent {result.byteCount} bytes after {PendingSerializations} send requests" : $"Sending failed, {PendingSerializations} send requests active")}");
#endif
            #endregion

            if (result.success) {
                PendingSerializations = 0;
                return;
            }

            if (AutoRetrySendOnFailure) {
                MarkNetworkDirty();
            }
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
#if TLP_DEBUG
            DebugLog("OnDeserialization");
#endif
        }
        #endregion

        #region Unity Lifecycle
        public virtual void Start() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Start));
#endif
            #endregion

            if (!SetupAndValidate()) {
                ErrorAndDisableGameObject(
                        $"Some dependencies are not set up correctly. " +
                        $"Deactivating GameObject '{transform.GetPathInScene()}'");
            }
        }
        #endregion

        #region Hooks
        /// <summary>
        /// Hook that is called during <see cref="Start"/> that shall be used to verify
        /// that e.g. all serialized references are setup correctly.
        /// </summary>
        /// <returns>shall return false if any essential reference is missing</returns>
        protected virtual bool SetupAndValidate() {
            if (GetLogger()) {
                return true;
            }

            if (MissingLoggerLogged()) {
                // no need to fail again on all the other scripts if the first
                // one already logged the error regarding the missing logger
                return true;
            }

            GloballyRememberMissingLoggerLogged();
            Debug.LogError(
                    $"{LOGPrefix}: No active {nameof(TlpLogger)} found. Please add the Prefab 'TLP_Logger' to your scene and make sure the GameObject is activated", this);
            return false;
        }

        private static void GloballyRememberMissingLoggerLogged() {
            Networking.LocalPlayer.SetPlayerTag(PlayerTagTlpLoggerMissingLogged, "true");
        }
        #endregion

        #region Logging
        private bool _hadLogger;
        private string LOGPrefix => $"[{ExecutionOrderReadOnly} {this.GetScriptPathInScene()}] ";


        protected TlpLogger Logger { private set; get; }

        protected void DebugLog(string message) {
#if TLP_DEBUG
            if ((int)Severity < (int)ELogLevel.Debug) {
                return;
            }

            if (GetLogger()) {
                Logger.DebugLog(LOGPrefix, message, ExecutionOrder, this);
            } else {
                Debug.Log(LOGPrefix + message, this);
            }
#endif
        }

        protected void Info(string message) {
            if ((int)Severity < (int)ELogLevel.Info) {
                return;
            }

            if (GetLogger()) {
                Logger.Info(LOGPrefix, message, this);
            } else {
                Debug.Log(LOGPrefix + message, this);
            }
        }

        protected void Warn(string message) {
            if ((int)Severity < (int)ELogLevel.Warning) {
                return;
            }

            if (GetLogger()) {
                Logger.Warn(LOGPrefix, message, this);
            } else {
                Debug.LogWarning(LOGPrefix + message, this);
            }
        }

        protected void ErrorAndDisableComponent(string message) {
            Error(message);
            enabled = false;
        }

        protected void ErrorAndDisableGameObject(string message) {
            Error(message);
            gameObject.SetActive(false);
        }

        protected void ErrorAndDisableComponentAndGameObject(string message) {
            Error(message);
            enabled = false;
            gameObject.SetActive(false);
        }

        protected void Error(string message) {
            if ((int)Severity < (int)ELogLevel.Assertion) {
                return;
            }

            if (GetLogger()) {
                Logger.Error(LOGPrefix, message, this);
            } else {
                Debug.LogError(LOGPrefix + message, this);
            }
        }

        private bool GetLogger() {
            if (Utilities.IsValid(Logger)) {
                return true;
            }

            var logger = GameObject.Find(TlpLogger.ExpectedGameObjectName());
            if (!Utilities.IsValid(logger)) {
                if (_hadLogger) {
#if TLP_DEBUG
                    Debug.LogWarning($"{LOGPrefix} : Logger is already destroyed", this);
#endif
                } else if (!MissingLoggerLogged()) {
                    GloballyRememberMissingLoggerLogged();
                    Debug.LogError($"{LOGPrefix}: No active TlpLogger found. Please add the Prefab '{TlpLogger.ExpectedGameObjectName()}' to your scene and make sure the GameObject is activated", this);
                }

                return false;
            }

            Logger = logger.GetComponent<TlpLogger>();
            _hadLogger = true;
            return Utilities.IsValid(Logger);
        }

        /// <summary>
        /// Notes: Has no effect unless the corresponding compiler flag is set in Unity via the menu
        /// <b>TLP/UdonUtils/Log Assertion Errors/Enable</b>.<br/>
        /// The option is saved in the Editor Preferences and will be the same across multiple Unity projects that use
        /// the same Unity version!
        ///<br/><br/>
        /// When active it will return false if the condition is false, it will log an error message filled
        /// with the name of the context object the error occurred on and with the given error message.
        /// </summary>
        /// <param name="condition">Condition expected to be true, false with log the error message</param>
        /// <param name="message">Compact error message, will be surrounded by context info</param>
        /// <param name="context">Object which is relevant to the condition failing, usually a behaviour or GameObject</param>
        /// <returns>The value of condition</returns>
        [Obsolete("Use Assert(bool condition, string message) instead")]
        protected bool Assert(bool condition, string message, Object context) {
#if !TLP_DEBUG
            return condition;
#else
            if ((int)Severity < (int)ELogLevel.Assertion) {
                return condition;
            }

            if (condition) {
                return true;
            }

            if (Utilities.IsValid(context)) {
                var udonSharpBehaviour = (UdonSharpBehaviour)context;
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (Utilities.IsValid(udonSharpBehaviour)) {
                    Error($"Assertion failed : '{udonSharpBehaviour.gameObject.name} : {message}'");
                } else {
                    Error($"Assertion failed : '{context.GetType()} : {message}'");
                }
            } else {
                Error("Assertion failed :  '" + message + "'");
            }

            Debug.Assert(condition, message);
            return false;
#endif
        }

        [Conditional("TLP_DEBUG")]
        protected void Assert(bool condition, string message) {
            if ((int)Severity < (int)ELogLevel.Assertion) {
                return;
            }

            if (condition) {
                return;
            }

            ErrorAndDisableComponentAndGameObject("Assertion failed :  '" + message + "'");
            Debug.Assert(condition, message);
        }
        #endregion

        #region Event listening
        [FormerlySerializedAs("eventInstigator")]
        [HideInInspector]
        [PublicAPI]
        public TlpBaseBehaviour EventInstigator;

        [PublicAPI]
        public virtual void OnEvent(string eventName) {
#if TLP_DEBUG
            DebugLog($"{nameof(OnEvent)} {eventName}");
#endif
            Error($"Unhandled event '{eventName}'");
        }
        #endregion

        #region Pool
        [FormerlySerializedAs("pool")]
        [PublicAPI]
        [HideInInspector]
        public TlpBaseBehaviour Pool;

        // true if retrieved from the pool (use the pool reference to return it)
        [PublicAPI]
        [HideInInspector]
        public bool PoolableInUse;

        /// <summary>
        /// Called after instantiation (before OnReadyForUse) by the pool if this is a new instances that was never in the pool.
        ///
        /// (Called regardless of whether the created instance GameObject is active or not)
        ///
        /// Note: Pooled objects returned from the pool have this method called before they are enabled!
        /// </summary>
        [PublicAPI]
        public virtual void OnCreated() {
#if TLP_DEBUG
            DebugLog(nameof(OnCreated));
#endif
        }

        /// <summary>
        /// Called when this instance is no longer controlled by the pool (got removed from the pool to be used)
        ///
        /// Note: Pooled objects returned from the pool have this method called before they are enabled!
        /// </summary>
        [PublicAPI]
        public virtual void OnReadyForUse() {
#if TLP_DEBUG
            DebugLog(nameof(OnReadyForUse));
#endif
        }

        /// <summary>
        /// Called by the pool just before the instance is returned to the pool.
        /// Shall be used to reset the state of this instance.
        ///
        /// Note: Pooled objects returning to the pool have this method called after they are disabled!
        /// </summary>
        [PublicAPI]
        public virtual void OnPrepareForReturnToPool() {
#if TLP_DEBUG
            DebugLog(nameof(OnPrepareForReturnToPool));
#endif
        }
        #endregion

        #region Internal
        private static bool MissingLoggerLogged() {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                return false;
            }

            return localPlayer.GetPlayerTag(PlayerTagTlpLoggerMissingLogged) == "true";
        }
        #endregion
    }
}