using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
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

        private const int InvalidInvocationFrame = -1;
        private const int InvalidIndex = -1;

        [SerializeField]
        internal TlpBaseBehaviour[] Listeners = { };

        public bool RaiseOnEnable;
        public bool RaiseOnStart;

        [Tooltip("Name of the method to be called on each listener")]
        [PublicAPI]
        public string ListenerMethod = "OnRaised";

        public TlpBaseBehaviour[] ListenersReadOnly => Listeners == null
                ? new TlpBaseBehaviour[0]
                : (TlpBaseBehaviour[])Listeners.Clone();

        public int ListenerCount { get; internal set; }

        private int _nextInvocationFrame;
        internal TlpBaseBehaviour _instigator;

        #region Unity Lifecycle
        public void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (RaiseOnEnable) {
                Raise(this);
            }
        }

        public override void Start() {
            base.Start();

            if (RaiseOnStart) {
                Raise(this);
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Removes the first found entry
        /// </summary>
        /// <param name="listener">must be valid</param>
        /// <returns>true if the listener is valid and was listening</returns>
        [PublicAPI]
        public bool RemoveListener(TlpBaseBehaviour listener, bool all = false) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RemoveListener));
#endif
            #endregion

            if (!Utilities.IsValid(listener)) {
                return false;
            }

            ListenerCount = Consolidate(Listeners, ListenerCount);
            int found = 0;
            for (int i = 0; i < ListenerCount; i++) {
                if (Listeners[i] != listener) {
                    continue;
                }

                Listeners[i] = null;
                ListenerCount = Consolidate(Listeners, ListenerCount);

                if (!all) {
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
        public virtual bool Raise(TlpBaseBehaviour instigator) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(Raise)} '{ListenerMethod}'");
#endif
            #endregion

            if (!Utilities.IsValid(instigator)) {
                Error("instigator must be valid");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ListenerMethod)) {
                Warn($"{nameof(ListenerMethod)} is empty, event {name} will not be raised");
                return false;
            }

            _instigator = instigator;
            IsPendingInvocation = false;

            foreach (var listener in Listeners) {
                if (!Utilities.IsValid(listener)) {
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
        public void RaiseExtern() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RaiseExtern));
#endif
            #endregion

            Raise(this);
        }

        /// <summary>
        /// DON'T use directly, use <see cref="RaiseOnIdle"/> instead
        /// </summary>
        public void InternalOnIdle() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(InternalOnIdle));
#endif
            #endregion

            if (!IsPendingInvocation) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog("already executed, skipping");
#endif
                #endregion

                return;
            }

            if (!Raise(_instigator)) {
                Error($"{nameof(InternalOnIdle)}: failed to raise '{ListenerMethod}'");
            }
        }

        /// <summary>
        /// Clears the listeners list by resetting the count making it effectively contain no entries
        /// </summary>
        [PublicAPI]
        public void RemoveAllListeners() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RemoveAllListeners));
#endif
            #endregion

            ListenerCount = 0;
        }

        /// <summary>
        /// Does not care about duplicates!
        /// </summary>
        /// <param name="listener">must be valid</param>
        /// <param name="callbackName">name of the function used as callback, used only to check correct connection</param>
        [PublicAPI]
        public bool AddListenerVerified(TlpBaseBehaviour listener, string callbackName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(AddListenerVerified)} '{callbackName}'");

#endif
            #endregion

            if (callbackName == ListenerMethod) {
                return AddListener(listener);
            }

            Error(
                    $"{nameof(AddListenerVerified)}: callback name mismatch, expected '{ListenerMethod}' but received '{callbackName}'"
            );
            return false;
        }

        [PublicAPI]
        public bool IsPendingInvocation { get; internal set; }

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
        ) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(RaiseOnIdle)} after {idleFrames} frames");
#endif
            #endregion


            if (idleFrames < 1) {
                Error($"{nameof(idleFrames)} must be at least 1 but was {idleFrames}");
                return false;
            }

            if (!Utilities.IsValid(instigator)) {
                Error("instigator must be valid");
                return false;
            }

            _instigator = instigator;
            int newPendingInvocation = Time.frameCount + idleFrames;
            if (Time.frameCount < _nextInvocationFrame && IsPendingInvocation &&
                newPendingInvocation >= _nextInvocationFrame) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Already pending raise of '{ListenerMethod}'");

#endif
                #endregion

                return true;
            }

            _nextInvocationFrame = newPendingInvocation;
            IsPendingInvocation = true;
            SendCustomEventDelayedFrames(nameof(InternalOnIdle), idleFrames, eventTiming);
            return true;
        }

        [PublicAPI]
        public int NextInvocationFrame => IsPendingInvocation ? _nextInvocationFrame : InvalidInvocationFrame;
        #endregion

        #region Internal
        private bool AddListener(TlpBaseBehaviour listener) {
#if TLP_DEBUG
            DebugLog(nameof(AddListener));
#endif
            if (!Utilities.IsValid(listener)) {
                return false;
            }

            if (Listeners == null) {
                Listeners = new TlpBaseBehaviour[1];
            } else if (ListenerCount >= Listeners.Length) {
                var tmp = new TlpBaseBehaviour[ListenerCount + 1];
                Listeners.CopyTo(tmp, 0);
                Listeners = tmp;
            }

            Listeners[ListenerCount] = listener;

            ++ListenerCount;
            return true;
        }


        private static int Consolidate(TlpBaseBehaviour[] list, int elements) {
            if (list == null) {
                return 0;
            }

            int end = Mathf.Min(elements, list.Length);
            int valid = 0;
            int moveIndex = InvalidIndex;
            for (int i = 0; i < end; i++) {
                if (Utilities.IsValid(list[i])) {
                    ++valid;
                    if (moveIndex == InvalidIndex) {
                        continue;
                    }

                    list[moveIndex] = list[i];
                    list[i] = null;
                    ++moveIndex;
                } else {
                    // ensure that the entry no longer references an invalid object
                    list[i] = null;
                    if (moveIndex == InvalidIndex) {
                        moveIndex = i;
                    }
                }
            }

            return valid;
        }
        #endregion
    }
}