using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

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
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class TestCase : TlpBaseBehaviour
    {
        [NonSerialized]
        public TestController TestController;

        [NonSerialized]
        internal TestResult Result;

        protected internal TestCaseStatus Status = TestCaseStatus.Ready;

        public void Initialize() {
            if (!TestController) {
                Error(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize: invalid test controller"
                );
                return;
            }

            Info("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize");
            InitializeTest();
        }

        public void Run() {
            if (!TestController) {
                Error(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run: invalid test controller"
                );
                return;
            }

            Info("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run");
            RunTest();
        }

        public void CleanUp() {
            if (!TestController) {
                Error(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp: invalid test controller"
                );
                return;
            }

            Info("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp");
            CleanUpTest();
        }


        #region Hooks
        protected virtual void InitializeTest() {
            // TODO your init behaviour here
            // ...

            // whenever the test is ready to be started call TestController.TestInitialized,
            // can be later in update or whenever but MUST be called at some point
            TestController.TestInitialized(true);
        }

        protected virtual void RunTest() {
            // TODO your test behaviour here
            // ...

            // whenever the test is completed call TestController.TestCompleted,
            // can be later in update or whenever but MUST be called at some point
            TestController.TestCompleted(true);
        }

        protected virtual void CleanUpTest() {
            // TODO your clean up behaviour here
            // ...

            // whenever the test is cleaned up call TestController.TestCleanedUp,
            // can be later in update or whenever but MUST be called at some point
            TestController.TestCleanedUp(true);
        }
        #endregion
    }
}