using System;
using System.Diagnostics;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Sources;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TLP.UdonUtils.Runtime.Logger
{
    public enum ELogLevel
    {
        None = 0,
        Assertion = 1,
        Error = 2,
        Warning = 3,
        Info = 4,
        Debug = 5
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TlpLogger), ExecutionOrder)]
    public class TlpLogger : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.Min + 1;

        #region Dependencies
        public TimeSource TimeSource;
        public FrameCountSource FrameCount;
        #endregion

        #region Settings
        public bool DetailedPlayerInfo = true;
        public bool DetailedContextInfo = true;

        [Tooltip(
                "If true will combine all Debug logs of a frame into a single string, can be used to see what has been logged in the entire frame. Can be useful to determine frames with excessive logging."
        )]
        public bool CreateDebugFrameLog;

        [SerializeField]
        [Tooltip(
                "If anything added only these scripts are allowed to log via this logger (will be skipped if also in blacklist)")]
        internal UdonSharpBehaviour[] WhiteList;

        [SerializeField]
        [Tooltip("Any script added here will not log anything via this logger")]
        internal UdonSharpBehaviour[] BlackList;

        /// <summary>
        /// If not empty only these scripts in here are allowed to use this Logger,
        /// if they are also in the RuntimeBlackList they won't log either.
        /// </summary>
        public readonly DataDictionary RuntimeWhiteList = new DataDictionary();

        /// <summary>
        /// Any script in this list is not allowed to log using this Logger.
        /// </summary>
        public readonly DataDictionary RuntimeBlackList = new DataDictionary();
        #endregion

        #region State
        private float _startTime;
        private int _lastFrame = -1;
        private readonly Stopwatch _performanceStopwatch = new Stopwatch();
        private readonly Stopwatch _frameTimeStopwatch = new Stopwatch();
        private float _lastLog;
        #endregion

        public void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(Prefix, nameof(OnEnable), ExecutionOrder, this);
#endif
            #endregion

            string expectedName = ExpectedGameObjectName();
            if (gameObject.name == expectedName) {
                return;
            }

            Warn(Prefix, $"Changing name of GameObject '{transform.GetPathInScene()}' to '{expectedName}'", this);
            gameObject.name = expectedName;
        }

        public void Update() {
            _frameTimeStopwatch.Restart();
        }

        public string DebugLogOfFrame { get; private set; }

        public const string DefaultPrefix = "[<color=#008000>TLP</color>]";
        protected virtual string Prefix => DefaultPrefix;

        protected virtual string GetPlayerInfo(Object context) {
            if (!DetailedPlayerInfo) {
                return "";
            }

            if (!Utilities.IsValid(TimeSource)) {
                return "";
            }

            float delta = TimeSource.Time() - _lastLog;
            _lastLog += delta;


            bool master = false;
            int localPlayerPlayerId = -1;
            bool isLocal = true;
            string playerName = "None";
            if (Utilities.IsValid(Networking.LocalPlayer)) {
                localPlayerPlayerId = Networking.LocalPlayer.playerId;
            }

            int ownerId = localPlayerPlayerId;
            // ReSharper disable once OperatorIsCanBeUsed not supported by U#
            if (Utilities.IsValid(context) && context.GetType() == typeof(UdonBehaviour)) {
                var udonBehaviour = (UdonBehaviour)context;
                var theOwner = Networking.GetOwner(udonBehaviour.gameObject);
                if (Utilities.IsValid(theOwner)) {
                    ownerId = theOwner.playerId;
                    master = theOwner.isMaster;
                    isLocal = theOwner.playerId == localPlayerPlayerId;
                    playerName = theOwner.displayName;
                }
            }

            double elapsedAccurate = _performanceStopwatch.Elapsed.TotalMilliseconds;
            _performanceStopwatch.Restart();
            return
                    $"[<color=#008080>f={FrameCount.Frame()}({_frameTimeStopwatch.Elapsed.TotalMilliseconds:F3}ms) elapsed={TimeSource.Time() - _startTime:F4}s time={TimeSource.Time():F4}s dt(real)={elapsedAccurate:F3}ms </color>][<color=#804040>{(isLocal ? "Local" : "Remote")} owner {playerName}({ownerId}) {(master ? " is Master" : "")}</color>]";
        }

        public virtual void DebugLog(string logPrefix, string message, int executionOrder, Object context) {
            if ((int)Severity < (int)ELogLevel.Debug) {
                return;
            }

            if (!AllowedToLog(context)) return;

            string completeMessage;
            if (DetailedContextInfo) {
                completeMessage =
                        $"[<color=#FF3B8B>DEBUG</color>]{Prefix}{logPrefix}{GetPlayerInfo(context)} {message}";
                Debug.Log(completeMessage, context);
            } else {
                completeMessage = $"[<color=#FF3B8B>DEBUG</color>]{GetPlayerInfo(context)} {message}";
                Debug.Log(completeMessage, context);
            }

            if (!CreateDebugFrameLog) {
                return;
            }

            if (FrameCount.Frame() == _lastFrame) {
                DebugLogOfFrame += $"{completeMessage}\n";
            } else {
                _lastFrame = FrameCount.Frame();
                DebugLogOfFrame = $"{completeMessage}\n";
            }
        }

        public virtual void Info(string logPrefix, string message, Object context) {
            if ((int)Severity < (int)ELogLevel.Info) {
                return;
            }

            if (!AllowedToLog(context)) return;
            Debug.Log(
                    DetailedContextInfo
                            ? $"[INFO]{Prefix}{logPrefix}{GetPlayerInfo(context)} {message}"
                            : $"[INFO]{GetPlayerInfo(context)} {message}",
                    context);
        }

        public virtual void Warn(string logPrefix, string message, Object context) {
            if ((int)Severity < (int)ELogLevel.Warning) {
                return;
            }

            if (!AllowedToLog(context)) return;
            Debug.LogWarning(
                    DetailedContextInfo
                            ? $"[<color=#FFFD55>WARN</color>]{Prefix}{logPrefix}{GetPlayerInfo(context)} {message}"
                            : $"[<color=#FFFD55>WARN</color>]{GetPlayerInfo(context)} {message}",
                    context
            );
        }

        public virtual void Error(string logPrefix, string message, Object context) {
            if ((int)Severity < (int)ELogLevel.Assertion) {
                return;
            }

            if (!AllowedToLog(context)) return;
            Debug.LogError(
                    DetailedContextInfo
                            ? $"[<color=#EB3324>ERROR</color>]{Prefix}{logPrefix}{GetPlayerInfo(context)} {message}"
                            : $"[<color=#EB3324>ERROR</color>]{GetPlayerInfo(context)} {message}",
                    context
            );
        }

        #region Hook Implementations
        public static string ExpectedGameObjectName() {
            return $"TLP_Logger";
        }

        protected override bool SetupAndValidate() {
            if (!Utilities.IsValid(TimeSource)) {
                Error($"{nameof(TimeSource)} is not set");
                return false;
            }

            if (!Utilities.IsValid(FrameCount)) {
                Error($"{nameof(FrameCount)} is not set");
                return false;
            }

            if (!base.SetupAndValidate()) {
                return false;
            }

            UpdateBlackAndWhiteListLookup();

            _startTime = TimeSource.Time();
            Info($"Starting at time {_startTime}s.");
            return true;
        }
        #endregion

        #region Internal
        private void UpdateBlackAndWhiteListLookup() {
            if (WhiteList != null) {
                foreach (var udonSharpBehaviour in WhiteList) {
                    RuntimeWhiteList[udonSharpBehaviour] = true;
                }
            }

            if (BlackList != null) {
                foreach (var udonSharpBehaviour in BlackList) {
                    RuntimeBlackList[udonSharpBehaviour] = true;
                }
            }
        }

        private bool AllowedToLog(Object context) {
            return context != null && (RuntimeWhiteList.Count == 0 || RuntimeWhiteList.ContainsKey(context)) &&
                   (RuntimeBlackList.Count == 0 || !RuntimeBlackList.ContainsKey(context));
        }
        #endregion

        #region Static Log Functions
        public static void StaticDebugLog(string message, Type type, Object context = null) {
#if TLP_DEBUG
            Debug.Log($"[<color=#FF3B8B>DEBUG</color>]{DefaultPrefix}[{type}] {message}", context);
#endif
        }

        public static void StaticInfo(string message, Type type, Object context = null) {
            Debug.Log($"[INFO]{DefaultPrefix}[{type}] {message}", context);
        }

        public static void StaticWarning(string message, Type type, Object context = null) {
            Debug.LogWarning($"[<color=#FFFD55>WARN</color>]{DefaultPrefix}[{type}] {message}", context);
        }

        public static void StaticError(string message, Type type, Object context = null) {
            Debug.LogError($"[<color=#EB3324>ERROR</color>]{DefaultPrefix}[{type}] {message}", context);
        }
        #endregion
    }
}