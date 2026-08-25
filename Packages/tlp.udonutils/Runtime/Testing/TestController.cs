using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestController), ExecutionOrder)]
    public class TestController : Controller
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestResult.ExecutionOrder + 1;

        private const string LogPrefix = "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>]";

        [Header("Start Input")]
        public bool OnlyMasterCanStart;

        public KeyCode StartKey0 = KeyCode.T;

        public KeyCode StartKey1 = KeyCode.E;

        public KeyCode StartKey2 = KeyCode.S;

        [Header("Abort Input")]
        public KeyCode AbortKey0 = KeyCode.LeftControl;

        public KeyCode AbortKey1 = KeyCode.C;

        [Header("Tests")]
        public bool StartOnLocalPlayerJoin;

        public bool StartOnOtherPlayerJoin;

        [SerializeField]
        private TestData TestData;

        [SerializeField]
        private TestResultsUi TestResultsUi;

        [SerializeField]
        private UdonEvent TestDataChangeEvent;

        private bool _isTesting;
        private bool _testInitialized;
        private bool _testCompleted;
        private bool _testCleanedUp;

        private int _testIndex;
        private bool _pendingNextStep;

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!_InitializeMvc(TestData, TestResultsUi, this, TestDataChangeEvent)) {
                _Error($"{LogPrefix} {name}.Start: failed to initialize MVC");
                return false;
            }

            return true;
        }

        protected override bool _InitializeInternal() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_InitializeInternal));
#endif
            #endregion

            var tests = transform.GetComponentsInChildren<TestCase>();

            foreach (var test in tests) {
                TestData.Tests.Add(test);
            }

            TestData.Dirty = true;

            return TestData._NotifyIfDirty(1) && base._InitializeInternal();
        }

        public override void OnPlayerJoined(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnPlayerJoined)} {player._ToStringSafe()}");
#endif
            #endregion


            if (OnlyMasterCanStart && !Networking.IsMaster) {
                _Info($"{LogPrefix} {name}.OnPlayerJoined: Only master can start test");
                return;
            }

            if (StartOnLocalPlayerJoin && player._IsLocalSafe()) {
                _Info($"{LogPrefix} {name}.OnPlayerJoined: starting after local player {player._ToStringSafe()} joined");
                // start the tests
                _isTesting = true;
                _ContinueTesting();
                return;
            }

            if (StartOnOtherPlayerJoin) {
                _Info($"{LogPrefix} {name}.OnPlayerJoined: starting after other player {player._ToStringSafe()} joined");
                // start the tests
                _isTesting = true;
                _ContinueTesting();
                return;
            }

            _Info($"{LogPrefix} {name}.OnPlayerJoined: not started due to settings");
        }

        internal void Update() {
            if (OnlyMasterCanStart && !Networking.IsMaster) {
                return;
            }

            if (_pendingNextStep) {
                _pendingNextStep = false;
                _ContinueTesting();
            }

            if (_isTesting) {
                if (Input.GetKey(AbortKey0)
                    && Input.GetKey(AbortKey1)) {
                    _AbortTestRun();
                }
            } else {
                if (Input.GetKey(StartKey0)
                    && Input.GetKey(StartKey1)
                    && Input.GetKey(StartKey2)) {
                    _StartTestRun();
                }
            }
        }

        public void _AbortTestRun() {
            _Info($"{LogPrefix} {name}.AbortTestRun: aborting after current test");

            for (++_testIndex; _testIndex < TestData.Tests.Count; ++_testIndex) {
                var testCase = (TestCase)TestData.Tests[_testIndex].Reference;
                testCase.Status = TestCaseStatus.NotRun;
            }

            // set the index to the lenght of the list of tests to prevent continuation
            _testIndex = TestData.Tests.Count;

            TestData.Dirty = true;
            TestData._NotifyIfDirty(1);
        }

        public void _StartTestRun() {
            if (_isTesting) {
                _Warn(
                        $"{LogPrefix} {name}.StartTestRun: can not start a new test while another " +
                        $"one is still running"
                );
                return;
            }

            _Info($"{LogPrefix} {name}.StartTestRun");

            _isTesting = true;
            _testIndex = 0;
            _pendingNextStep = true;

            for (int i = 0; i < TestData.Tests.Count; ++i) {
                var testCase = (TestCase)TestData.Tests[i].Reference;
                testCase.Status = TestCaseStatus.Ready;
                TestData.Dirty = true;
                TestData._NotifyIfDirty(1);
            }
        }

        private void _ContinueTesting() {
            int numberOfTests = TestData.Tests.Count;
            if (numberOfTests > 0 && _testIndex > -1 && _testIndex < numberOfTests) {
                var testCase = (TestCase)TestData.Tests[_testIndex].Reference;
                if (!testCase) {
                    _Error($"{LogPrefix} {name}.ContinueTesting: tests contains invalid behaviour");
                    return;
                }

                var context = testCase.gameObject;
                if (!_testInitialized) {
                    _Info($"{LogPrefix} {name}.ContinueTesting: Initializing test {context}");
                    testCase.TestController = this;
                    testCase._Initialize();
                    return;
                }

                if (!_testCompleted) {
                    _Info($"{LogPrefix} {name}.ContinueTesting: Running test {context}");
                    testCase._Run();
                    return;
                }

                if (!_testCleanedUp) {
                    _Info($"{LogPrefix} {name}.ContinueTesting: Cleaning up test {context}");
                    testCase._CleanUp();
                    return;
                }

                ++_testIndex;
                _isTesting = _testIndex < TestData.Tests.Count;

                _testInitialized = false;
                _testCompleted = false;
                _testCleanedUp = false;

                _pendingNextStep = _isTesting;

                if (!_isTesting) {
                    _Info($"{LogPrefix} {name}.ContinueTesting: All test completed");
                }
            } else {
                _Warn($"{LogPrefix} {name}.ContinueTesting: test run aborted");
                _testIndex = 0;
                _isTesting = false;
                _testInitialized = false;
                _testCompleted = false;
                _testCleanedUp = false;
                _pendingNextStep = false;
            }
        }

        public void _TestInitialized(bool success) {
            var testCase = (TestCase)TestData.Tests[_testIndex].Reference;
            if (success) {
                _Info($"{LogPrefix} {name}.TestInitialized: Initialized test successfully");
                testCase.Status = TestCaseStatus.Running;
            } else {
                _Error($"{LogPrefix} {name}.TestInitialized: Test initialization failed");
                testCase.Status = TestCaseStatus.Failed;
            }

            TestData.Dirty = true;
            TestData._NotifyIfDirty(1);

            _testInitialized = true;
            if (!success) {
                _testCompleted = true;
            }

            _pendingNextStep = true;
        }

        public void _TestCompleted(bool success) {
            var testCase = (TestCase)TestData.Tests[_testIndex].Reference;

            if (success) {
                _Info($"{LogPrefix} {name}.TestCompleted: PASS");
                testCase.Status = TestCaseStatus.Passed;
            } else {
                _Error($"{LogPrefix} {name}.TestCompleted: FAIL");
                testCase.Status = TestCaseStatus.Failed;
            }

            TestData.Dirty = true;
            TestData._NotifyIfDirty(1);

            _testCompleted = true;
            _pendingNextStep = true;
        }

        public void _TestCleanedUp(bool success) {
            var testCase = (TestCase)TestData.Tests[_testIndex].Reference;
            if (success) {
                _Info($"{LogPrefix} {name}.TestCleanedUp: Cleaned up test successfully");
            } else {
                _Error($"{LogPrefix} {name}.TestCleanedUp: Test cleanup failed");
                testCase.Status = TestCaseStatus.Failed;
            }

            TestData.Dirty = true;
            TestData._NotifyIfDirty(1);

            // ensure we don't get any more messages from the test
            testCase.TestController = null;

            _testCleanedUp = true;
            _pendingNextStep = true;
        }
    }
}