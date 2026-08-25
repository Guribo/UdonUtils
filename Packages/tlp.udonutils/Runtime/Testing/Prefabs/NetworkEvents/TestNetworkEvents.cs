using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonUtils.Runtime.Tests
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestNetworkEvents), ExecutionOrder)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TestNetworkEvents : TestCase
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestOwnershipTransfer.ExecutionOrder + 1;
        #endregion

        #region State
        // encodes which events have been called, for example if event 0 and 2 have been called, the value will be 0b00000101
        private uint _calledEvents;

        /// <summary>
        /// Expected events that should be callable via SendCustomNetworkEvent:
        ///   0: public, no _, no params              → legacy callable
        ///   4: [NC] public, no _                    → modern path
        ///   5: [NC] public, with _                  → modern path (underscore ignored)
        ///   6: [NC] public, with params              → modern path
        ///   7: [NC] public, with _ and params        → modern path
        /// All others: blocked (underscore, params without [NC], private, protected,
        ///            or [NetworkCallable] on non-public — compiler rejects [NC] on internal/private/protected)
        /// </summary>
        private const uint ExpectedCalledEvents = 0b0000000011110001;
        #endregion

        protected override void _InitializeTest() {
            _calledEvents = 0;

            if (VRCPlayerApi.GetPlayerCount() != 2) {
                _Error("Network events test requires exactly 2 players");
                TestController._TestInitialized(false);
                return;
            }

            TestController._TestInitialized(true);
        }

        protected override void _RunTest() {
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_RPC_StartTest));
            SendCustomEventDelayedSeconds(nameof(_Delayed_TestTimeout), 5f);
        }

        #region RPCs
        [NetworkCallable]
        public void _RPC_StartTest() {
            _EnsureLocalOwnership();

            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo0));
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo1));
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo2), 42);
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo3), 42);
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo4));
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo5));
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo6), 42);
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo7), 42);
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo8));
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo9));
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo10), 42);
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo11), 42);
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo12));  // [NetworkCallable] requires public
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo13));  // [NetworkCallable] requires public
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo14), 42);  // [NetworkCallable] requires public
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo15), 42);  // [NetworkCallable] requires public
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo16));
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo17));
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo18), 42);
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo19), 42);
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo20));  // [NetworkCallable] requires public
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo21));  // [NetworkCallable] requires public
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo22), 42);  // [NetworkCallable] requires public
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo23), 42);  // [NetworkCallable] requires public
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo24));
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo25));
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo26), 42);
            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo27), 42);
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo28));  // [NetworkCallable] requires public
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo29));  // [NetworkCallable] requires public
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(Foo30), 42);  // [NetworkCallable] requires public
            // SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(_Foo31), 42);  // [NetworkCallable] requires public
        }
        #endregion

        #region DelayedEvents
        public void _Delayed_TestTimeout() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Delayed_TestTimeout));
#endif
            #endregion

            if (!HasStartedOk || !Utilities.IsValid(TestController)) {
                _Error("TestController not valid or test not started, cannot complete test");
                return;
            }

            if (Status != TestCaseStatus.Running) {
                return;
            }

            if (_calledEvents == 0) {
                _Error("Network events test timed out");
                TestController._TestCompleted(false);
                return;
            }

            _Info($"Network events test completed, called events: " +
                  $"{Convert.ToString(_calledEvents, 2).PadLeft(32, '0')}, " +
                  $"expected: {Convert.ToString(ExpectedCalledEvents, 2).PadLeft(32, '0')}");

            TestController._TestCompleted(_calledEvents == ExpectedCalledEvents);
        }
        #endregion

        #region NetworkEventsUnderTest
        public void Foo0() {
            SetCalledEvent(0);
        }

        public void _Foo1() {
            SetCalledEvent(1);
        }

        // ReSharper disable once UnusedParameter.Global
        public void Foo2(int bar) {
            SetCalledEvent(2);
        }

        // ReSharper disable once UnusedParameter.Global
        public void _Foo3(int bar) {
            SetCalledEvent(3);
        }

        [NetworkCallable]
        public void Foo4() {
            SetCalledEvent(4);
        }

        [NetworkCallable]
        public void _Foo5() {
            SetCalledEvent(5);
        }

        [NetworkCallable]
        // ReSharper disable once UnusedParameter.Global
        public void Foo6(int bar) {
            SetCalledEvent(6);
        }

        [NetworkCallable]
        // ReSharper disable once UnusedParameter.Global
        public void _Foo7(int bar) {
            SetCalledEvent(7);
        }

        internal void Foo8() {
            SetCalledEvent(8);
        }

        internal void _Foo9() {
            SetCalledEvent(9);
        }

        // ReSharper disable once UnusedParameter.Global
        internal void Foo10(int bar) {
            SetCalledEvent(10);
        }

        // ReSharper disable once UnusedParameter.Global
        internal void _Foo11(int bar) {
            SetCalledEvent(11);
        }

        // [NetworkCallable] requires public — cannot be applied to internal/private/protected
        // [NetworkCallable]
        // internal void Foo12() {
        //     SetCalledEvent(12);
        // }
        //
        // [NetworkCallable]
        // internal void _Foo13() {
        //     SetCalledEvent(13);
        // }
        //
        // [NetworkCallable]
        // ReSharper disable once UnusedParameter.Global
        // internal void Foo14(int bar) {
        //     SetCalledEvent(14);
        // }
        //
        // [NetworkCallable]
        // ReSharper disable once UnusedParameter.Global
        // internal void _Foo15(int bar) {
        //     SetCalledEvent(15);
        // }

        private void Foo16() {
            SetCalledEvent(16);
        }

        private void _Foo17() {
            SetCalledEvent(17);
        }

        // ReSharper disable once UnusedParameter.Local
        private void Foo18(int bar) {
            SetCalledEvent(18);
        }

        // ReSharper disable once UnusedParameter.Local
        private void _Foo19(int bar) {
            SetCalledEvent(19);
        }

        // [NetworkCallable] requires public — cannot be applied to internal/private/protected
        // [NetworkCallable]
        // private void Foo20() {
        //     SetCalledEvent(20);
        // }
        //
        // [NetworkCallable]
        // private void _Foo21() {
        //     SetCalledEvent(21);
        // }
        //
        // [NetworkCallable]
        // ReSharper disable once UnusedParameter.Local
        // private void Foo22(int bar) {
        //     SetCalledEvent(22);
        // }
        //
        // [NetworkCallable]
        // ReSharper disable once UnusedParameter.Local
        // private void _Foo23(int bar) {
        //     SetCalledEvent(23);
        // }

        protected void Foo24() {
            SetCalledEvent(24);
        }

        protected void _Foo25() {
            SetCalledEvent(25);
        }

        // ReSharper disable once UnusedParameter.Local
        protected void Foo26(int bar) {
            SetCalledEvent(26);
        }

        // ReSharper disable once UnusedParameter.Local
        protected void _Foo27(int bar) {
            SetCalledEvent(27);
        }

        // [NetworkCallable] requires public — cannot be applied to internal/private/protected
        // [NetworkCallable]
        // protected void Foo28() {
        //     SetCalledEvent(28);
        // }
        //
        // [NetworkCallable]
        // protected void _Foo29() {
        //     SetCalledEvent(29);
        // }
        //
        // [NetworkCallable]
        // ReSharper disable once UnusedParameter.Local
        // protected void Foo30(int bar) {
        //     SetCalledEvent(30);
        // }
        //
        // [NetworkCallable]
        // ReSharper disable once UnusedParameter.Local
        // protected void _Foo31(int bar) {
        //     SetCalledEvent(31);
        // }
        #endregion

        #region Internal
        private void SetCalledEvent(int index) {
            _calledEvents |= (uint)1 << index;
        }
        #endregion
    }
}