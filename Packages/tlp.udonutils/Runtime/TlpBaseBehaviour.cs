using System;
using System.Diagnostics;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
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
    [TlpDefaultExecutionOrder(typeof(TlpBaseBehaviour), ExecutionOrder)]
    public abstract class TlpBaseBehaviour : UdonSharpBehaviour
    {
        public virtual int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public const int ExecutionOrder = TlpExecutionOrder.DefaultStart + 1;

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

        /// <summary>
        /// <inheritdoc cref="SyncPaused"/>
        /// </summary>
        [SerializeField]
        [Tooltip("Has no effect unless implemented by this particular script. See SyncPaused property for details.")]
        private bool PauseSynchronization;

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
        /// <summary>
        /// Can be used to pause any synchronization locally, only works if inheriting scripts
        /// check against this variable in <see cref="OnPreSerialization"/> and <see cref="OnDeserialization"/>!
        /// Additionally, use local variables to store received values instead of directly using synced variables!
        ///
        /// When un-paused, will automatically send the current state or request a refresh from the current owner.
        /// Pausing is local only and does not affect other players.
        /// </summary>
        public bool SyncPaused
        {
            get => PauseSynchronization;
            set
            {
                PauseSynchronization = value;
                if (PauseSynchronization) {
                    return;
                }

                if (Networking.IsOwner(gameObject)) {
                    MarkNetworkDirty();
                    return;
                }

                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(RPC_RequestSerialization));
            }
        }

        protected VRCPlayerApi LocalPlayer { get; private set; }

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

            if (SyncPaused) {
                Warn($"Synchronization is paused, set {nameof(SyncPaused)} to false first");
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
            DebugLog(
                    $"{nameof(OnDeserialization)}: s{deserializationResult.sendTime}s; r{deserializationResult.receiveTime}s; d{deserializationResult.Latency()}s");
#endif
        }

        /// <summary>
        /// Ensures that the local player owns the network instance of the current game object.
        /// If the local player does not currently have ownership, ownership is set to them.
        /// </summary>
        /// <returns>
        /// Returns true if the local player successfully owns the network instance of the game object,
        /// false if ownership is not successfully established or the behavior is not initialized correctly.
        /// </returns>
        public bool EnsureLocalOwnership() {
            if (!HasStartedOk) {
                return false;
            }

            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(LocalPlayer, gameObject);
            }

            if (Networking.IsOwner(gameObject)) {
                return true;
            }

            Error($"{this.GetScriptPathInScene()} can not be owned by local player");
            return false;
        }

        #region RPCs
        /// <summary>
        /// Called by remote clients when they unpause synchronization
        /// </summary>
        public void RPC_RequestSerialization() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RPC_RequestSerialization));
#endif
            #endregion

            bool unused = MarkNetworkDirty();
        }
        #endregion
        #endregion

        #region Unity Lifecycle
        #region State
        #region Start
        /// <summary>
        /// Whether Unity has called the Start method
        /// </summary>
        public bool IsReceivingStart { private set; get; }

        public bool HasReceivedStart { protected set; get; }

        public bool HasStartedOk
        {
            get
            {
                if (_hasStartedSuccessfully) {
                    return true;
                }

                if (HasReceivedStart) {
                    Error($"Accessing {nameof(HasStartedOk)} after validation failed");
                    return false;
                }

                if (IsReceivingStart) {
                    Error($"Accessing {nameof(HasStartedOk)} during {nameof(Start)}");
                    return false;
                }

                if (!Utilities.IsValid(Networking.LocalPlayer)) {
                    Error($"{nameof(HasStartedOk)}: No active {nameof(Networking.LocalPlayer)}");
                    return false;
                }

#if TLP_DEBUG
                _isEarlyStart = true;
#endif
                Start();
#if TLP_DEBUG
                _isEarlyStart = false;
#endif
                return _hasStartedSuccessfully;
            }
            set
            {
                if (!value) {
                    HasReceivedStart = false;
                }

                _hasStartedSuccessfully = value;
            }
        }

        private bool _hasStartedSuccessfully;
#if TLP_DEBUG
        private bool _isEarlyStart;
#endif
        #endregion


#if UNITY_INCLUDE_TESTS && !COMPILER_UDONSHARP
        public bool IsActiveAndEnabled => Utilities.IsValid(this) && enabled;
#else
        public bool IsActiveAndEnabled => Utilities.IsValid(this) && enabled && gameObject.activeInHierarchy;
#endif

        public virtual bool HasFailedToStart => (IsReceivingStart || HasReceivedStart) && !HasStartedOk;
        #endregion

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public void Start() {
#else
        internal void Start() {
#endif

            if (HasReceivedStart) {
                return;
            }

            IsReceivingStart = true;

            #region TLP_DEBUG
#if TLP_DEBUG
            if (!_isEarlyStart) {
                DebugLog(nameof(Start));
                if (Utilities.IsValid(Networking.LocalPlayer)) {
                    AssertExecutionOrderCorrect();
                }
            } else {
                DebugLog($"{nameof(Start)} (early)");
            }
#endif
            #endregion

            LocalPlayer = Networking.LocalPlayer;
            if (SetupAndValidate() && IsSet(LocalPlayer, nameof(LocalPlayer))) {
                HasStartedOk = true;
            } else {
                HasStartedOk = false;
                ErrorAndDisableGameObject(
                        $"Some dependencies are not set up correctly. " +
                        $"Deactivating GameObject '{transform.GetPathInScene()}'");
            }

            IsReceivingStart = false;
            HasReceivedStart = true;
        }
        #endregion

        #region Hooks
        /// <summary>
        /// Hook that is called during <see cref="Start"/> that shall be used to verify
        /// that e.g. all serialized references are set up correctly.
        /// </summary>
        /// <returns>shall return false if any essential reference is missing</returns>
        protected virtual bool SetupAndValidate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(SetupAndValidate));
#endif
            #endregion

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
                    $"{LOGPrefix}: No active {nameof(TlpLogger)} found. Please add the Prefab 'TLP_Logger' to your scene and make sure the GameObject is activated",
                    this);
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

        public void DebugLog(string message) {
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

        public void Info(string message) {
            if ((int)Severity < (int)ELogLevel.Info) {
                return;
            }

            if (GetLogger()) {
                Logger.Info(LOGPrefix, message, this);
            } else {
                Debug.Log(LOGPrefix + message, this);
            }
        }

        public void Warn(string message) {
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

        public void Error(string message) {
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
                    Debug.LogError(
                            $"{LOGPrefix}: No active TlpLogger found. Please add the Prefab '{TlpLogger.ExpectedGameObjectName()}' to your scene and make sure the GameObject is activated",
                            this);
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
        [Obsolete("Use Assert(bool condition, string message) instead", true)]
        protected bool Assert(bool condition, string message, Object context) {
            return false;
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
            DebugLog_OnEvent(eventName);
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
        protected bool IsSet(object variable, string variableName) {
            if (Utilities.IsValid(variable)) {
                return true;
            }

            Error($"{variableName} is not set");
            return false;
        }

        private static bool MissingLoggerLogged() {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                return false;
            }

            return localPlayer.GetPlayerTag(PlayerTagTlpLoggerMissingLogged) == "true";
        }

        private void AssertExecutionOrderCorrect() {
            const string playerTagExecOrder = "TLP/Base/ExecOrder";
            const string playerTagLastFrameStart = "TLP/Base/LastFrameStart";
            const string playerTagLastFrameBehaviour = "TLP/Base/LastFrameBehaviour";
            string lastStarted = Networking.LocalPlayer.GetPlayerTag(playerTagExecOrder);
            string lastFrameStart = Networking.LocalPlayer.GetPlayerTag(playerTagLastFrameStart);
            string lastFrameBehaviour = Networking.LocalPlayer.GetPlayerTag(playerTagLastFrameBehaviour);
            ValidateStartOrder(lastFrameStart, lastStarted, lastFrameBehaviour);
            Networking.LocalPlayer.SetPlayerTag(playerTagExecOrder, ExecutionOrderReadOnly.ToString());
            Networking.LocalPlayer.SetPlayerTag(playerTagLastFrameStart, Time.frameCount.ToString());
            Networking.LocalPlayer.SetPlayerTag(playerTagLastFrameBehaviour, GetUdonTypeName());
        }

        private void ValidateStartOrder(string lastFrameStart, string lastStarted, string lastBehaviour) {
            if (string.IsNullOrEmpty(lastFrameStart)) {
                return;
            }

            if (!int.TryParse(lastFrameStart, out int savedFrame)) {
                return;
            }

            if (savedFrame != Time.frameCount) {
                return;
            }

            if (string.IsNullOrEmpty(lastStarted)) {
                return;
            }

            if (!int.TryParse(lastStarted, out int execOrder)) {
                return;
            }

            if (lastBehaviour != GetUdonTypeName() && ExecutionOrderReadOnly <= execOrder) {
                Warn(
                        $"Received Start() in incorrect order, previously started script '{lastBehaviour}' in this frame had " +
                        $"ExecutionOrder {execOrder} while {GetUdonTypeName()} has ExecutionOrder {ExecutionOrderReadOnly}. " +
                        $"Please add [TlpDefaultExecutionOrder(ExecutionOrder)] to this script or the script that started before this one.");
            }
        }
        #endregion

        #region Debugging
#if TLP_DEBUG
        protected void DebugLog_OnEvent(string eventName) {
            DebugLog($"{nameof(OnEvent)}: {eventName} (Instigator: {EventInstigator.GetScriptPathInScene()})");
        }

#endif
        #endregion
    }
}