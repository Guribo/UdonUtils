using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Sources;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace TLP.UdonUtils.Logger
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

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class TlpLogger : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.Min;

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
        #endregion

        #region State
        private float _startTime;
        private int _lastFrame = -1;
        private readonly System.Diagnostics.Stopwatch _performanceStopwatch = new System.Diagnostics.Stopwatch();
        private readonly System.Diagnostics.Stopwatch _frameTimeStopwatch = new System.Diagnostics.Stopwatch();
        private float _lastLog;
        #endregion


        public void Update() {
            _frameTimeStopwatch.Restart();
        }

        public string DebugLogOfFrame { get; private set; }

        protected virtual string Prefix => "[<color=#008000>TLP</color>]";

        protected virtual string GetPlayerInfo(Object context) {
            if (!DetailedPlayerInfo) {
                return "";
            }

            if (!Utilities.IsValid(TimeSource)) return "";
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

            Debug.LogError(
                    DetailedContextInfo
                            ? $"[<color=#EB3324>ERROR</color>]{Prefix}{logPrefix}{GetPlayerInfo(context)} {message}"
                            : $"[<color=#EB3324>ERROR</color>]{GetPlayerInfo(context)} {message}",
                    context
            );
        }

        #region Hook Implementations
        protected override bool SetupAndValidate() {
            if (!Utilities.IsValid(TimeSource)) {
                Error($"{nameof(TimeSource)} is not set");
                return false;
            }

            if (!Utilities.IsValid(FrameCount)) {
                Error($"{nameof(FrameCount)} is not set");
                return false;
            }

            if (!base.SetupAndValidate()) return false;
            _startTime = TimeSource.Time();
            Info($"Starting at time {_startTime}s.");
            return true;
        }
        #endregion
    }
}