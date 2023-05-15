using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Enums;

namespace TLP.UdonUtils.Runtime.Events
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UdonEvent : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DefaultStart + 1;


        // ReSharper disable once UseArrayEmptyMethod
        [FormerlySerializedAs("listeners")]
        [SerializeField]
        internal TlpBaseBehaviour[] Listeners = new TlpBaseBehaviour[0];

        public TlpBaseBehaviour[] ListenersReadOnlyCopy =>
            Listeners == null ? null : (TlpBaseBehaviour[])Listeners.Clone();

        public int ListenerCount { get; internal set; }

        public bool RaiseOnEnable;
        public bool RaiseOnStart;

        public void OnEnable()
        {
            if (RaiseOnEnable)
            {
                Raise(this);
            }
        }

        public void Start()
        {
            if (RaiseOnStart)
            {
                Raise(this);
            }
        }

        #region Public API

        [FormerlySerializedAs("listenerMethod")]
        [Tooltip("Name of the method to be called on each listener")]
        [PublicAPI]
        public string ListenerMethod = "OnRaised";

        private int _pendingInvocation;
        private bool _isPendingInvocation;
        private TlpBaseBehaviour _instigator;

        /// <summary>
        /// Removes the first found entry
        /// </summary>
        /// <param name="listener">must be valid</param>
        [PublicAPI]
        public bool RemoveListener(TlpBaseBehaviour listener, bool all = false)
        {
#if TLP_DEBUG
            DebugLog(nameof(RemoveListener));
#endif
            if (!Utilities.IsValid(listener))
            {
                return false;
            }

            ListenerCount = Consolidate(Listeners, ListenerCount);
            int found = 0;
            for (int i = 0; i < ListenerCount; i++)
            {
                if (Listeners[i] != listener)
                {
                    continue;
                }

                Listeners[i] = null;
                ListenerCount = Consolidate(Listeners, ListenerCount);

                if (!all)
                {
                    return true;
                }

                ++found;
            }

            return found > 0;
        }

        /// <summary>
        /// Calls the method specified by <see cref="ListenerMethod"/> on each valid listener.
        ///
        /// Note: listeners are notified in the order they are added.
        /// </summary>
        [PublicAPI]
        [RecursiveMethod]
        public virtual bool Raise(TlpBaseBehaviour instigator)
        {
            _instigator = instigator;
#if TLP_DEBUG
            DebugLog($"{nameof(Raise)} '{ListenerMethod}'");
#endif
            if (!Utilities.IsValid(instigator))
            {
                Error("instigator must be valid");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ListenerMethod))
            {
                Warn($"{nameof(ListenerMethod)} is empty, event {name} will not be raised");
                return false;
            }

            _isPendingInvocation = false;

            foreach (var listener in Listeners)
            {
                if (!Utilities.IsValid(listener))
                {
                    continue;
                }

                listener.EventInstigator = instigator;
                listener.OnEvent(ListenerMethod);
            }

            return true;
        }

        /// <summary>
        /// Use this when calling from e.g. an Unity UI event like Button or Scroll-rect
        /// </summary>
        [PublicAPI]
        public void RaiseExtern()
        {
            Raise(this);
        }

        /// <summary>
        /// DON'T use directly, use <see cref="RaiseOnIdle"/> instead
        /// </summary>
        public void OnIdle()
        {
#if TLP_DEBUG
            DebugLog(nameof(OnIdle));
#endif
            if (!_isPendingInvocation)
            {
                DebugLog("already executed, skipping");
                return;
            }

            Raise(_instigator);
        }

        /**
         * Clears the listeners list by resetting the count making it effectively contain no entries
         */
        [PublicAPI]
        public void RemoveAllListeners()
        {
#if TLP_DEBUG
            DebugLog(nameof(RemoveAllListeners));
#endif
            ListenerCount = 0;
        }

        /// <summary>
        /// Does not care about duplicates!
        /// </summary>
        /// <param name="listener">must be valid</param>
        /// <param name="callbackName">name of the function used as callback, used only to check correct connection</param>
        [PublicAPI]
        public bool AddListenerVerified(TlpBaseBehaviour listener, string callbackName)
        {
#if TLP_DEBUG
            DebugLog(nameof(AddListenerVerified) + " " + callbackName);
#endif
            if (callbackName == ListenerMethod)
            {
                return AddListener(listener);
            }

            Error(
                $"{nameof(AddListenerVerified)}: callback name mismatch: expected '{ListenerMethod}' but received '{callbackName}'"
            );
            return false;
        }

        [PublicAPI]
        public bool IsPendingInvocation()
        {
#if TLP_DEBUG
            DebugLog(nameof(IsPendingInvocation));
#endif
            return _isPendingInvocation;
        }


        /// <summary>
        /// raises the event after the number of frames, unless the event was raised before by another source.
        /// 
        /// </summary>
        /// <param name="idleFrames">0 and 1 are equivalent and will perform execution on the next update</param>
        /// <exception cref="NotImplementedException"></exception>
        [PublicAPI]
        public virtual bool RaiseOnIdle(
            TlpBaseBehaviour instigator,
            int idleFrames = 1,
            EventTiming eventTiming = EventTiming.Update
        )
        {
#if TLP_DEBUG
            DebugLog(nameof(RaiseOnIdle));
#endif
            if (idleFrames < 1)
            {
                Error($"{nameof(idleFrames)} must be at least 1 but was {idleFrames}");
                return false;
            }

            if (!Utilities.IsValid(instigator))
            {
                Error("instigator must be valid");
                return false;
            }

            _instigator = instigator;
            int newPendingInvocation = Time.frameCount + idleFrames;
            if (Time.frameCount < _pendingInvocation && _isPendingInvocation &&
                newPendingInvocation >= _pendingInvocation)
            {
#if TLP_DEBUG
                DebugLog($"Already pending raise of '{ListenerMethod}'");
#endif
                return true;
            }

            _pendingInvocation = newPendingInvocation;
            _isPendingInvocation = true;
            SendCustomEventDelayedFrames(nameof(OnIdle), idleFrames, eventTiming);
            return true;
        }


        [PublicAPI]
        public int GetScheduledExecution()
        {
#if TLP_DEBUG
            DebugLog(nameof(GetScheduledExecution));
#endif
            return _isPendingInvocation ? _pendingInvocation : -1;
        }

        #endregion

        #region Internal

        private bool AddListener(TlpBaseBehaviour listener)
        {
#if TLP_DEBUG
            DebugLog(nameof(AddListener));
#endif
            if (!Utilities.IsValid(listener))
            {
                return false;
            }

            if (Listeners == null)
            {
                Listeners = new TlpBaseBehaviour[1];
            }
            else if (ListenerCount >= Listeners.Length)
            {
                var tmp = new TlpBaseBehaviour[ListenerCount + 1];
                Listeners.CopyTo(tmp, 0);
                Listeners = tmp;
            }

            Listeners[ListenerCount] = listener;

            ++ListenerCount;
            return true;
        }


        private static int Consolidate(TlpBaseBehaviour[] list, int elements)
        {
            if (list == null)
            {
                return 0;
            }

            int end = Mathf.Min(elements, list.Length);

            int valid = 0;
            int moveIndex = -1;
            for (int i = 0; i < end; i++)
            {
                if (Utilities.IsValid(list[i]))
                {
                    ++valid;
                    if (moveIndex == -1)
                    {
                        continue;
                    }

                    list[moveIndex] = list[i];
                    list[i] = null;
                    ++moveIndex;
                }
                else
                {
                    // ensure that the entry no longer references an invalid object
                    list[i] = null;
                    if (moveIndex == -1)
                    {
                        moveIndex = i;
                    }
                }
            }

            return valid;
        }

        #endregion
    }
}