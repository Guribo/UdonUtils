using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Common;
using TLP.UdonUtils.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils
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
        public const string TlpLoggerGameObjectName = "TLP_Logger";

        #endregion

        #region Networking

        [PublicAPI]
        public bool IsPendingSerialization()
        {
            return PendingSerializations > 0;
        }

        [PublicAPI]
        public bool DropPendingSerializations()
        {
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
        public bool MarkNetworkDirty()
        {
#if TLP_DEBUG
            DebugLog(nameof(MarkNetworkDirty));
#endif
            if (!Networking.IsOwner(gameObject))
            {
                Warn("Can not mark network dirty, not owner");
                return false;
            }

            PendingSerializations = Math.Max(1, PendingSerializations + 1);
            SendCustomEventDelayedFrames(nameof(ExecuteScheduledSerialization), 0);
            return true;
        }

        public void ExecuteScheduledSerialization()
        {
#if TLP_DEBUG
            DebugLog(nameof(ExecuteScheduledSerialization));
#endif
            if (PendingSerializations < 1)
            {
                #region TLP_DEBUG

#if TLP_DEBUG
                DebugLog("Nothing to do");
#endif

                #endregion

                return;
            }

            if (!Networking.IsOwner(gameObject))
            {
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

        public override void OnPreSerialization()
        {
#if TLP_DEBUG
            DebugLog(nameof(OnPreSerialization));
#endif
            if (PendingSerializations < 1)
            {
                PendingSerializations = 1;
            }
        }

        public override void OnPostSerialization(SerializationResult result)
        {
#if TLP_DEBUG
            DebugLog(nameof(OnPostSerialization));
#endif
            if (result.success)
            {
#if TLP_DEBUG
                DebugLog(
                    $"{nameof(OnPostSerialization)} wrote {result.byteCount} bytes of {PendingSerializations} serialization requests to the network"
                );
#endif
                PendingSerializations = 0;
                return;
            }

            MarkNetworkDirty();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult)
        {
#if TLP_DEBUG
            DebugLog("OnDeserialization");
#endif
        }

        #endregion

        #region Logging

        [FormerlySerializedAs("severity")]
        public ELogLevel Severity = ELogLevel.Debug;

        private string LOGPrefix =>
            $"[{ExecutionOrderReadOnly} {gameObject.name}/{UdonCommon.UdonTypeNameShort(GetUdonTypeName())}]";


        protected TlpLogger Logger { private set; get; }

        protected void DebugLog(string message)
        {
#if TLP_DEBUG
            if ((int)Severity < (int)ELogLevel.Debug)
            {
                return;
            }

            if (GetLogger())
            {
                Logger.DebugLog(LOGPrefix, message, ExecutionOrder, this);
            }
            else
            {
                Debug.Log(LOGPrefix + message, this);
            }
#endif
        }

        protected void Info(string message)
        {
            if ((int)Severity < (int)ELogLevel.Info)
            {
                return;
            }

            if (GetLogger())
            {
                Logger.Info(LOGPrefix, message, this);
            }
            else
            {
                Debug.Log(LOGPrefix + message, this);
            }
        }

        protected void Warn(string message)
        {
            if ((int)Severity < (int)ELogLevel.Warning)
            {
                return;
            }

            if (GetLogger())
            {
                Logger.Warn(LOGPrefix, message, this);
            }
            else
            {
                Debug.LogWarning(LOGPrefix + message, this);
            }
        }

        protected void ErrorAndDisableComponent(string message)
        {
            Error(message);
            enabled = false;
        }

        protected void ErrorAndDisableGameObject(string message)
        {
            Error(message);
            gameObject.SetActive(false);
        }

        protected void Error(string message)
        {
            if ((int)Severity < (int)ELogLevel.Assertion)
            {
                return;
            }

            if (GetLogger())
            {
                Logger.Error(LOGPrefix, message, this);
            }
            else
            {
                Debug.LogError(LOGPrefix + message, this);
            }
        }

        private bool GetLogger()
        {
            if (Utilities.IsValid(Logger))
            {
                return true;
            }

            var logger = GameObject.Find(TlpLoggerGameObjectName);
            if (!Utilities.IsValid(logger))
            {
                Debug.LogError(LOGPrefix + " : Logger does not exist in the scene or is already destroyed", this);
                return false;
            }

            Logger = logger.GetComponent<TlpLogger>();

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
        protected bool Assert(bool condition, string message, UnityEngine.Object context)
        {
#if !TLP_DEBUG
            return condition;
#else
            if ((int)Severity < (int)ELogLevel.Assertion)
            {
                return condition;
            }

            if (condition)
            {
                return true;
            }

            if (Utilities.IsValid(context))
            {
                var udonSharpBehaviour = (UdonSharpBehaviour)context;
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (Utilities.IsValid(udonSharpBehaviour))
                {
                    Error($"Assertion failed : '{udonSharpBehaviour.gameObject.name} : {message}'");
                }
                else
                {
                    Error($"Assertion failed : '{context.GetType()} : {message}'");
                }
            }
            else
            {
                Error("Assertion failed :  '" + message + "'");
            }

            Debug.Assert(condition, message);
            return false;
#endif
        }

        #endregion

        #region Event listening

        [FormerlySerializedAs("eventInstigator")]
        [HideInInspector]
        [PublicAPI]
        public TlpBaseBehaviour EventInstigator;

        [PublicAPI]
        public virtual void OnEvent(string eventName)
        {
#if TLP_DEBUG
            DebugLog($"{nameof(OnEvent)} {eventName}");
#endif
            Error($"Unhandled event '{eventName}'");
        }

        #endregion

        #region Pool

        #region UdonPool Interface

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
        public virtual void OnCreated()
        {
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
        public virtual void OnReadyForUse()
        {
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
        public virtual void OnPrepareForReturnToPool()
        {
#if TLP_DEBUG
            DebugLog(nameof(OnPrepareForReturnToPool));
#endif
        }

        #endregion

        #endregion
    }
}