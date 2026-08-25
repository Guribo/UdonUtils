using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UdonSharpEditor;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonUtils.Editor.Core
{
    /// <summary>
    /// Custom base editor to be used to easily provide some description to a script
    /// </summary>
    public abstract class TlpBehaviourEditor : UnityEditor.Editor
    {
        protected abstract string _GetDescription();

        public override void OnInspectorGUI() {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target, false, false)) return;
            EditorGUILayout.HelpBox(_GetDescription(), MessageType.Info);
            base.OnInspectorGUI();
        }
    }
}
#endif

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

        #region State
        internal int PendingSerializations;
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
                    _MarkNetworkDirty();
                    return;
                }

                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_RPC_RequestSerialization));
            }
        }

        protected VRCPlayerApi LocalPlayer { get; private set; }

        [PublicAPI]
        public bool _IsPendingSerialization() {
            return PendingSerializations > 0;
        }

        [PublicAPI]
        public bool _DropPendingSerializations() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_DropPendingSerializations));
#endif
            #endregion

            bool hadPending = _IsPendingSerialization();
            PendingSerializations = 0;
            return hadPending;
        }


        [PublicAPI]
        public bool _MarkNetworkDirty() {
#if TLP_DEBUG
            _DebugLog(nameof(_MarkNetworkDirty));
#endif
            if (!Networking.IsOwner(gameObject)) {
                _Warn("Can not mark network dirty, not owner");
                return false;
            }

            if (SyncPaused) {
                _Warn($"Synchronization is paused, set {nameof(SyncPaused)} to false first");
                return false;
            }

            PendingSerializations = Math.Max(1, PendingSerializations + 1);
            SendCustomEventDelayedFrames(nameof(_ExecuteScheduledSerialization), 0);
            return true;
        }

        public void _ExecuteScheduledSerialization() {
#if TLP_DEBUG
            _DebugLog(nameof(_ExecuteScheduledSerialization));
#endif
            if (PendingSerializations < 1) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog("Nothing to do");
#endif
                #endregion

                return;
            }

            if (!Networking.IsOwner(gameObject)) {
                PendingSerializations = 0;

                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog("Not owner");
#endif
                #endregion

                return;
            }

            RequestSerialization();
        }

        public override void OnPreSerialization() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnPreSerialization));
#endif
            #endregion

            if (PendingSerializations < 1) {
                PendingSerializations = 1;
            }
        }

        public override void OnPostSerialization(SerializationResult result) {
            #region TLP_DEBUG
#if TLP_DEBUG
            string statusMessage = result.success
                    ? $"Sent {result.byteCount} bytes after {PendingSerializations} send requests"
                    : $"Sending failed, {PendingSerializations} send requests active";
            _DebugLog($"{nameof(OnPostSerialization)}: {statusMessage}");
#endif
            #endregion

            if (result.success) {
                PendingSerializations = 0;
                return;
            }

            if (AutoRetrySendOnFailure) {
                _MarkNetworkDirty();
            }
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnDeserialization)}: VRC: " +
                      $"sendTime={deserializationResult.sendTime:F6}s; " +
                      $"receiveTime={deserializationResult.receiveTime:F6}s; " +
                      $"latency={deserializationResult._Latency() * 1000:F3}ms");
#endif
            #endregion
        }

        /// <summary>
        /// Ensures that the local player owns the network instance of the current game object.
        /// If the local player does not currently have ownership, ownership is set to them.
        /// </summary>
        /// <returns>
        /// Returns true if the local player successfully owns the network instance of the game object,
        /// false if ownership is not successfully established or the behavior is not initialized correctly.
        /// </returns>
        public bool _EnsureLocalOwnership() {
            if (!HasStartedOk) {
                return false;
            }

            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(LocalPlayer, gameObject);
            }

            if (Networking.IsOwner(gameObject)) {
                return true;
            }

            _Error($"{this._GetScriptPathInScene()} can not be owned by local player");
            return false;
        }

        #region RPCs
        /// <summary>
        /// Called by remote clients when they unpause synchronization
        /// </summary>
        [NetworkCallable(1)]
        public void _RPC_RequestSerialization() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RPC_RequestSerialization));
#endif
            #endregion

            bool unused = _MarkNetworkDirty();
        }
        #endregion
        #endregion

        #region Unity Lifecycle
        #region State
        #region Start
        /// <summary>
        /// Whether Unity has called the Start method
        /// </summary>
        protected bool IsReceivingStart { get; private set; }

        public bool HasReceivedStart { get; protected set; }

        public bool HasStartedOk
        {
            get
            {
                if (_hasStartedSuccessfully) {
                    return true;
                }

                if (HasReceivedStart) {
                    _Error($"Accessing {nameof(HasStartedOk)} after validation failed");
                    return false;
                }

                if (IsReceivingStart) {
                    _Error($"Accessing {nameof(HasStartedOk)} during {nameof(Start)}");
                    return false;
                }

                if (!Utilities.IsValid(Networking.LocalPlayer)) {
                    _Error($"{nameof(HasStartedOk)}: No active {nameof(Networking.LocalPlayer)}");
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

        public bool HasFailedToStart => (IsReceivingStart || HasReceivedStart) && !HasStartedOk;
        #endregion

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public void Start() {
#else
        internal void Start()
        {
#endif
            if (HasReceivedStart) {
                return;
            }

            IsReceivingStart = true;

            #region TLP_DEBUG
#if TLP_DEBUG
            if (!_isEarlyStart) {
                _DebugLog(nameof(Start));
                if (Utilities.IsValid(Networking.LocalPlayer)) {
                    _AssertExecutionOrderCorrect();
                }
            } else {
                _DebugLog($"{nameof(Start)} (early)");
            }
#endif
            #endregion

            LocalPlayer = Networking.LocalPlayer;
            if (_SetupAndValidate() && _IsSet(LocalPlayer, nameof(LocalPlayer))) {
                HasStartedOk = true;
            } else {
                HasStartedOk = false;
                _ErrorAndDisableGameObject(
                        $"Some dependencies are not set up correctly. " +
                        $"Deactivating GameObject '{transform._GetPathInScene()}'");
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
        protected virtual bool _SetupAndValidate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_SetupAndValidate));
#endif
            #endregion

            if (_GetLogger()) {
                return true;
            }

            if (_MissingLoggerLogged()) {
                // no need to fail again on all the other scripts if the first
                // one already logged the error regarding the missing logger
                return true;
            }

            _GloballyRememberMissingLoggerLogged();
            Debug.LogError(
                    $"{LOGPrefix}: No active {nameof(TlpLogger)} found. Please add the Prefab 'TLP_Logger' to your scene and make sure the GameObject is activated",
                    this);
            return false;
        }

        private static void _GloballyRememberMissingLoggerLogged() {
            Networking.LocalPlayer.SetPlayerTag(PlayerTagTlpLoggerMissingLogged, "true");
        }
        #endregion

        #region Logging
        private bool _hadLogger;
        private string LOGPrefix => $"[{ExecutionOrderReadOnly} {this._GetScriptPathInScene()}] ";


        protected TlpLogger Logger { private set; get; }

        public void _DebugLog(string message) {
#if TLP_DEBUG
            if ((int)Severity < (int)ELogLevel.Debug) {
                return;
            }

            if (_GetLogger()) {
                Logger.DebugLog(LOGPrefix, message, ExecutionOrder, this);
            } else {
                Debug.Log(LOGPrefix + message, this);
            }
#endif
        }

        protected void _Info(string message) {
            if ((int)Severity < (int)ELogLevel.Info) {
                return;
            }

            if (_GetLogger()) {
                Logger.Info(LOGPrefix, message, this);
            } else {
                Debug.Log(LOGPrefix + message, this);
            }
        }

        protected void _Warn(string message) {
            if ((int)Severity < (int)ELogLevel.Warning) {
                return;
            }

            if (_GetLogger()) {
                Logger.Warn(LOGPrefix, message, this);
            } else {
                Debug.LogWarning(LOGPrefix + message, this);
            }
        }

        protected void _ErrorAndDisableComponent(string message) {
            _Error(message);
            enabled = false;
        }

        protected void _ErrorAndDisableGameObject(string message) {
            _Error(message);
            gameObject.SetActive(false);
        }

        private void _ErrorAndDisableComponentAndGameObject(string message) {
            _Error(message);
            enabled = false;
            gameObject.SetActive(false);
        }

        protected void _Error(string message) {
            if ((int)Severity < (int)ELogLevel.Assertion) {
                return;
            }

            if (_GetLogger()) {
                Logger.Error(LOGPrefix, message, this);
            } else {
                Debug.LogError(LOGPrefix + message, this);
            }
        }

        private bool _GetLogger() {
            if (Utilities.IsValid(Logger)) {
                return true;
            }

            var logger = GameObject.Find(TlpLogger.ExpectedGameObjectName());
            if (!Utilities.IsValid(logger)) {
                if (_hadLogger) {
#if TLP_DEBUG
                    Debug.LogWarning($"{LOGPrefix} : Logger is already destroyed", this);
#endif
                } else if (!_MissingLoggerLogged()) {
                    _GloballyRememberMissingLoggerLogged();
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

        protected void _Assert(bool condition, string message) {
            if ((int)Severity < (int)ELogLevel.Assertion) {
                return;
            }

            if (condition) {
                return;
            }

            _ErrorAndDisableComponentAndGameObject("Assertion failed :  '" + message + "'");
            Debug.Assert(condition, message);
        }
        #endregion

        #region Event listening
        [FormerlySerializedAs("eventInstigator")]
        [HideInInspector]
        [PublicAPI]
        public TlpBaseBehaviour EventInstigator;

        [PublicAPI]
        public virtual void _OnEvent(string eventName) {
#if TLP_DEBUG
            _DebugLog_OnEvent(eventName);
#endif
            _Error($"Unhandled event '{eventName}'");
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
        public virtual void _OnCreated() {
#if TLP_DEBUG
            _DebugLog(nameof(_OnCreated));
#endif
        }

        /// <summary>
        /// Called when this instance is no longer controlled by the pool (got removed from the pool to be used)
        ///
        /// Note: Pooled objects returned from the pool have this method called before they are enabled!
        /// </summary>
        [PublicAPI]
        public virtual void _OnReadyForUse() {
#if TLP_DEBUG
            _DebugLog(nameof(_OnReadyForUse));
#endif
        }

        /// <summary>
        /// Called by the pool just before the instance is returned to the pool.
        /// Shall be used to reset the state of this instance.
        ///
        /// Note: Pooled objects returning to the pool have this method called after they are disabled!
        /// </summary>
        [PublicAPI]
        public virtual void _OnPrepareForReturnToPool() {
#if TLP_DEBUG
            _DebugLog(nameof(_OnPrepareForReturnToPool));
#endif
        }
        #endregion

        #region Internal
        protected bool _IsSet(object variable, string variableName) {
            if (Utilities.IsValid(variable)) {
                return true;
            }

            _Error($"{variableName} is not set");
            return false;
        }

        private static bool _MissingLoggerLogged() {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                return false;
            }

            return localPlayer.GetPlayerTag(PlayerTagTlpLoggerMissingLogged) == "true";
        }

        private void _AssertExecutionOrderCorrect() {
            const string playerTagExecOrder = "TLP/Base/ExecOrder";
            const string playerTagLastFrameStart = "TLP/Base/LastFrameStart";
            const string playerTagLastFrameBehaviour = "TLP/Base/LastFrameBehaviour";
            string lastStarted = Networking.LocalPlayer.GetPlayerTag(playerTagExecOrder);
            string lastFrameStart = Networking.LocalPlayer.GetPlayerTag(playerTagLastFrameStart);
            string lastFrameBehaviour = Networking.LocalPlayer.GetPlayerTag(playerTagLastFrameBehaviour);
            _ValidateStartOrder(lastFrameStart, lastStarted, lastFrameBehaviour);
            Networking.LocalPlayer.SetPlayerTag(playerTagExecOrder, ExecutionOrderReadOnly.ToString());
            Networking.LocalPlayer.SetPlayerTag(playerTagLastFrameStart, Time.frameCount.ToString());
            Networking.LocalPlayer.SetPlayerTag(playerTagLastFrameBehaviour, GetUdonTypeName());
        }

        private void _ValidateStartOrder(string lastFrameStart, string lastStarted, string lastBehaviour) {
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
                _Warn(
                        $"Received Start() in incorrect order, previously started script '{lastBehaviour}' in this frame had " +
                        $"ExecutionOrder {execOrder} while {GetUdonTypeName()} has ExecutionOrder {ExecutionOrderReadOnly}. " +
                        "Please add [TlpDefaultExecutionOrder(ExecutionOrder)] to this script or the script that started before this one.");
            }
        }
        #endregion

        #region Debugging
#if TLP_DEBUG
        protected void _DebugLog_OnEvent(string eventName) {
            _DebugLog($"{nameof(_OnEvent)}: {eventName} (Instigator: {EventInstigator._GetScriptPathInScene()})");
        }

#endif
        #endregion
    }
}