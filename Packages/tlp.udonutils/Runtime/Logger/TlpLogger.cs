using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

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

        private int _startTime;

        [FormerlySerializedAs("detailedPlayerInfo")]
        public bool DetailedPlayerInfo = true;

        [FormerlySerializedAs("detailedContextInfo")]
        public bool DetailedContextInfo = true;

        [FormerlySerializedAs("createDebugFrameLog")]
        [Tooltip(
            "If true will combine all Debug logs of a frame into a single string, can be used to see what has been logged in the entire frame. Can be useful to determine frames with excessive logging."
        )]
        public bool CreateDebugFrameLog;

        private int _lastFrame = -1;
        public string DebugLogOfFrame { get; private set; }

        private readonly System.Diagnostics.Stopwatch _performanceStopwatch = new System.Diagnostics.Stopwatch();

        public void Start()
        {
            _startTime = Networking.GetServerTimeInMilliseconds();

            Info(
                $"Starting at server time {_startTime}. Initialization took at least {_performanceStopwatch.Elapsed.TotalMilliseconds}ms."
            );
        }

        private int _lastLog;

        protected virtual string Prefix => "[<color=#008000>TLP</color>]";

        protected virtual string GetPlayerInfo(Object context)
        {
            if (!DetailedPlayerInfo)
            {
                return "";
            }

            int delta = Networking.GetServerTimeInMilliseconds() - _lastLog;
            _lastLog += delta;


            bool master = false;
            int localPlayerPlayerId = -1;
            bool isLocal = true;
            string playerName = "None";
            if (Utilities.IsValid(Networking.LocalPlayer))
            {
                localPlayerPlayerId = Networking.LocalPlayer.playerId;
            }

            int ownerId = localPlayerPlayerId;
            // ReSharper disable once OperatorIsCanBeUsed not supported by U#
            if (Utilities.IsValid(context) && context.GetType() == typeof(UdonBehaviour))
            {
                var udonBehaviour = (UdonBehaviour)context;
                var theOwner = Networking.GetOwner(udonBehaviour.gameObject);
                if (Utilities.IsValid(theOwner))
                {
                    ownerId = theOwner.playerId;
                    master = theOwner.isMaster;
                    isLocal = theOwner.playerId == localPlayerPlayerId;
                    playerName = theOwner.displayName;
                }
            }

            double elapsedAccurate = _performanceStopwatch.Elapsed.TotalMilliseconds;
            _performanceStopwatch.Restart();
            return
                $"[<color=#008080>f={Time.frameCount} ntss={Networking.GetServerTimeInMilliseconds() - _startTime}ms nt={Networking.GetServerTimeInMilliseconds()}ms dt={elapsedAccurate:F3}ms</color>][<color=#804040>{(isLocal ? "Local" : "Remote")} owner {playerName}({ownerId}) {(master ? " is Master" : "")}</color>]";
        }

        public virtual void DebugLog(string logPrefix, string message, int executionOrder, Object context)
        {
            if ((int)Severity < (int)ELogLevel.Debug)
            {
                return;
            }

            string completeMessage;
            if (DetailedContextInfo)
            {
                completeMessage =
                    $"[<color=#FF3B8B>DEBUG</color>]{Prefix}{logPrefix}{GetPlayerInfo(context)} {message}";
                Debug.Log(completeMessage, context);
            }
            else
            {
                completeMessage = $"[<color=#FF3B8B>DEBUG</color>]{GetPlayerInfo(context)} {message}";
                Debug.Log(completeMessage, context);
            }

            if (!CreateDebugFrameLog)
            {
                return;
            }

            if (Time.frameCount == _lastFrame)
            {
                DebugLogOfFrame += $"{completeMessage}\n";
            }
            else
            {
                _lastFrame = Time.frameCount;
                DebugLogOfFrame = $"{completeMessage}\n";
            }
        }

        public virtual void Info(string logPrefix, string message, Object context)
        {
            if ((int)Severity < (int)ELogLevel.Info)
            {
                return;
            }

            if (DetailedContextInfo)
            {
                Debug.Log($"[INFO]{Prefix}{logPrefix}{GetPlayerInfo(context)} {message}", context);
            }
            else
            {
                Debug.Log($"[INFO]{GetPlayerInfo(context)} {message}", context);
            }
        }

        public virtual void Warn(string logPrefix, string message, Object context)
        {
            if ((int)Severity < (int)ELogLevel.Warning)
            {
                return;
            }

            if (DetailedContextInfo)
            {
                Debug.LogWarning(
                    $"[<color=#FFFD55>WARN</color>]{Prefix}{logPrefix}{GetPlayerInfo(context)} {message}",
                    context
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[<color=#FFFD55>WARN</color>]{GetPlayerInfo(context)} {message}",
                    context
                );
            }
        }

        public virtual void Error(string logPrefix, string message, Object context)
        {
            if ((int)Severity < (int)ELogLevel.Assertion)
            {
                return;
            }

            if (DetailedContextInfo)
            {
                Debug.LogError(
                    $"[<color=#EB3324>ERROR</color>]{Prefix}{logPrefix}{GetPlayerInfo(context)} {message}",
                    context
                );
            }
            else
            {
                Debug.LogError(
                    $"[<color=#EB3324>ERROR</color>]{GetPlayerInfo(context)} {message}",
                    context
                );
            }
        }
    }
}