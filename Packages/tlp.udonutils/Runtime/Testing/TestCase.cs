using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Testing
{
    public enum TestCaseStatus
    {
        Ready,
        Running,
        Passed,
        Failed,
        NotRun
    }

    /// <summary>
    /// Component which implements the base of a test case, includes preparation, execution and cleanup methods
    /// to be copied to new test scripts and filled for each individual test case.
    /// 
    /// Behaviour sync mode can be changed depending on the test performed, default is no variable sync
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestCase), ExecutionOrder)]
    public abstract class TestCase : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestController.ExecutionOrder + 1;

        [NonSerialized]
        public TestController TestController;

        [NonSerialized]
        internal TestResult Result;

        protected internal TestCaseStatus Status = TestCaseStatus.Ready;

        public void _Initialize() {
            if (!TestController) {
                _Error(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize: invalid test controller"
                );
                return;
            }

            _Info("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize");
            _InitializeTest();
        }

        public void _Run() {
            if (!TestController) {
                _Error(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run: invalid test controller"
                );
                return;
            }

            _Info("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run");
            _RunTest();
        }

        public void _CleanUp() {
            if (!TestController) {
                _Error(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp: invalid test controller"
                );
                return;
            }

            _Info("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp");
            _CleanUpTest();
        }


        #region Hooks
        protected virtual void _InitializeTest() {
            // TODO your init behaviour here
            // ...

            // whenever the test is ready to be started call TestController.TestInitialized,
            // can be later in update or whenever but MUST be called at some point
            TestController._TestInitialized(true);
        }

        protected virtual void _RunTest() {
            // TODO your test behaviour here
            // ...

            // whenever the test is completed call TestController.TestCompleted,
            // can be later in update or whenever but MUST be called at some point
            TestController._TestCompleted(true);
        }

        protected virtual void _CleanUpTest() {
            // TODO your clean up behaviour here
            // ...

            // whenever the test is cleaned up call TestController.TestCleanedUp,
            // can be later in update or whenever but MUST be called at some point
            TestController._TestCleanedUp(true);
        }
        #endregion
    }
}