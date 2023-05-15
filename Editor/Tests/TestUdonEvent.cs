using System;
using NUnit.Framework;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Logger;
using UnityEngine;
using VRC.Udon.Common.Enums;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP")]
    public class TestUdonEvent : TestWithLogger
    {
        private UdonEvent m_UdonEvent;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            m_UdonEvent = new GameObject("UdonEvent").AddComponent<UdonEvent>();
        }

        #region AddListener

        [Test]
        public void AddListener_IncrementsCounterAndReturnsTrue()
        {
            Assert.True(m_UdonEvent.AddListenerVerified(m_UdonEvent, m_UdonEvent.ListenerMethod));
            Assert.AreEqual(1, m_UdonEvent.ListenerCount);

            Assert.True(m_UdonEvent.AddListenerVerified(m_UdonEvent, m_UdonEvent.ListenerMethod));
            Assert.AreEqual(2, m_UdonEvent.ListenerCount);
        }

        [Test]
        public void AddListener_InvalidListenerReturnsFalseWithoutIncrementing()
        {
            Assert.True(m_UdonEvent.AddListenerVerified(m_UdonEvent, m_UdonEvent.ListenerMethod));
            Assert.AreEqual(1, m_UdonEvent.ListenerCount);

            Assert.False(m_UdonEvent.AddListenerVerified(null, m_UdonEvent.ListenerMethod));
            Assert.AreEqual(1, m_UdonEvent.ListenerCount);
        }

        [Test]
        public void AddListener_IncreasesTheSizeOfTheArrayOfListenersIfNotEnoughSpace()
        {
            var a = m_UdonEvent.gameObject.AddComponent<UdonEvent>();
            var b = m_UdonEvent.gameObject.AddComponent<UdonEvent>();

            Assert.True(m_UdonEvent.AddListenerVerified(a, m_UdonEvent.ListenerMethod));
            Assert.AreEqual(1, m_UdonEvent.Listeners.Length);

            Assert.True(m_UdonEvent.AddListenerVerified(b, m_UdonEvent.ListenerMethod));
            Assert.AreEqual(2, m_UdonEvent.Listeners.Length);

            Assert.True(ReferenceEquals(a, m_UdonEvent.Listeners[0]));
            Assert.True(ReferenceEquals(b, m_UdonEvent.Listeners[1]));

            m_UdonEvent.ListenerCount = 4;
            Assert.True(m_UdonEvent.AddListenerVerified(m_UdonEvent, m_UdonEvent.ListenerMethod));
            Assert.AreEqual(5, m_UdonEvent.Listeners.Length);
            Assert.True(ReferenceEquals(m_UdonEvent, m_UdonEvent.Listeners[4]));
        }

        [Test]
        public void AddListener_DoesNotIncreaseSizeIfListenerCountSmallerThanArray()
        {
            m_UdonEvent.Listeners = new TlpBaseBehaviour[2];
            m_UdonEvent.ListenerCount = 0;

            Assert.True(m_UdonEvent.AddListenerVerified(m_UdonEvent, m_UdonEvent.ListenerMethod));
            Assert.AreEqual(1, m_UdonEvent.ListenerCount);
            Assert.AreEqual(2, m_UdonEvent.Listeners.Length);
            Assert.True(ReferenceEquals(m_UdonEvent, m_UdonEvent.Listeners[0]));
            Assert.True(ReferenceEquals(null, m_UdonEvent.Listeners[1]));
        }

        #endregion

        #region RemoveListener

        [Test]
        public void RemoveListener_DecrementsCounterAndReturnsTrue()
        {
            Assert.True(m_UdonEvent.AddListenerVerified(m_UdonEvent, m_UdonEvent.ListenerMethod));
            Assert.AreEqual(1, m_UdonEvent.ListenerCount);

            Assert.True(m_UdonEvent.RemoveListener(m_UdonEvent));
            Assert.AreEqual(0, m_UdonEvent.ListenerCount);
        }

        [Test]
        public void RemoveListener_InvalidListenerReturnsFalseWithoutDecrementing()
        {
            Assert.True(m_UdonEvent.AddListenerVerified(m_UdonEvent, m_UdonEvent.ListenerMethod));
            Assert.AreEqual(1, m_UdonEvent.ListenerCount);

            Assert.False(m_UdonEvent.RemoveListener(null));
            Assert.AreEqual(1, m_UdonEvent.ListenerCount);
        }

        [Test]
        public void RemoveListener_ReturnsFalseIfListenersNullOrEmpty()
        {
            m_UdonEvent.Listeners = Array.Empty<TlpBaseBehaviour>();
            Assert.False(m_UdonEvent.RemoveListener(m_UdonEvent));

            m_UdonEvent.Listeners = null;
            Assert.False(m_UdonEvent.RemoveListener(m_UdonEvent));
        }

        [Test]
        public void RemoveListener_DoesNotDecreaseTheSizeOfTheArray()
        {
            var a = m_UdonEvent.gameObject.AddComponent<UdonEvent>();
            var b = m_UdonEvent.gameObject.AddComponent<UdonEvent>();

            Assert.True(m_UdonEvent.AddListenerVerified(a, m_UdonEvent.ListenerMethod));
            Assert.True(m_UdonEvent.AddListenerVerified(b, m_UdonEvent.ListenerMethod));

            Assert.True(m_UdonEvent.RemoveListener(b));
            Assert.AreEqual(1, m_UdonEvent.ListenerCount);
            Assert.AreEqual(2, m_UdonEvent.Listeners.Length);
            Assert.True(ReferenceEquals(a, m_UdonEvent.Listeners[0]));
            Assert.True(ReferenceEquals(null, m_UdonEvent.Listeners[1]));
        }

        [Test]
        public void RemoveListener_MovesRemainingListenersTowardsBeginning()
        {
            var a = m_UdonEvent.gameObject.AddComponent<UdonEvent>();
            var b = m_UdonEvent.gameObject.AddComponent<UdonEvent>();
            var c = m_UdonEvent.gameObject.AddComponent<UdonEvent>();
            var d = m_UdonEvent.gameObject.AddComponent<UdonEvent>();

            Assert.True(m_UdonEvent.AddListenerVerified(a, m_UdonEvent.ListenerMethod));
            Assert.True(m_UdonEvent.AddListenerVerified(b, m_UdonEvent.ListenerMethod));
            Assert.True(m_UdonEvent.AddListenerVerified(c, m_UdonEvent.ListenerMethod));
            Assert.True(m_UdonEvent.AddListenerVerified(d, m_UdonEvent.ListenerMethod));

            // set it to 3 to verify that the 4th element is not consolidated
            m_UdonEvent.ListenerCount = 3;

            Assert.True(m_UdonEvent.RemoveListener(a));
            Assert.AreEqual(2, m_UdonEvent.ListenerCount);
            Assert.AreEqual(4, m_UdonEvent.Listeners.Length);
            Assert.True(ReferenceEquals(b, m_UdonEvent.Listeners[0]));
            Assert.True(ReferenceEquals(c, m_UdonEvent.Listeners[1]));
            Assert.True(ReferenceEquals(null, m_UdonEvent.Listeners[2]));

            // verify d was not consolidated due to modified listenerCount
            Assert.True(ReferenceEquals(d, m_UdonEvent.Listeners[3]));
        }

        #endregion

        [Test]
        public void MarkDirtySchedulesDelayedInvocation()
        {
            m_UdonEvent.RaiseOnIdle(m_UdonEvent, 1);
            Assert.True(m_UdonEvent.IsPendingInvocation());
        }

        [Test]
        public void RaisingEventWhenPendingInvocationClearsPendingStatus()
        {
            m_UdonEvent.RaiseOnIdle(m_UdonEvent, 1);
            m_UdonEvent.RaiseExtern();
            Assert.False(m_UdonEvent.IsPendingInvocation());
        }

        [Test]
        public void CallingRaiseOnIdleAgainWhenAlreadyPendingHasNoEffect()
        {
            m_UdonEvent.RaiseOnIdle(m_UdonEvent, 1);
            m_UdonEvent.RaiseOnIdle(m_UdonEvent, 10);
            m_UdonEvent.RaiseOnIdle(m_UdonEvent, 100);
            Assert.True(m_UdonEvent.IsPendingInvocation());
            Assert.AreEqual(Time.frameCount + 1, m_UdonEvent.GetScheduledExecution());
        }

        [Test]
        public void IfNotPendingExecutionFrameNumberIsMinus1()
        {
            Assert.AreEqual(-1, m_UdonEvent.GetScheduledExecution());
        }

        [Test]
        public void IfPendingExecutionFrameNumberIsTheFrameNumberOfTargetedExecution()
        {
            m_UdonEvent.RaiseOnIdle(m_UdonEvent, 10);
            Assert.AreEqual(Time.frameCount + 10, m_UdonEvent.GetScheduledExecution());
        }

        [Test]
        public void AfterExecutionScheduledFrameNumberIsMinusOneAgain()
        {
            m_UdonEvent.RaiseOnIdle(m_UdonEvent, 100);
            m_UdonEvent.RaiseExtern();
            Assert.AreEqual(-1, m_UdonEvent.GetScheduledExecution());
        }

        [Test]
        public void OnIdleDoesNothingIfNotPendingDelayedInvocation()
        {
            m_UdonEvent.RaiseOnIdle(m_UdonEvent, 100);
            m_UdonEvent.RaiseExtern(); // event is invoked manually before On Idle was invoked causing the status to be cleared
            TlpLogger.Severity = ELogLevel.Debug;
            ExpectLog("already executed, skipping");
            m_UdonEvent.OnIdle();
        }
    }
}