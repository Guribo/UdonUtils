using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class TestController : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.Max;

        private const string LogPrefix = "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>]";

        [FormerlySerializedAs("onlyMasterCanStart")]
        [Header("Start Input")]
        public bool OnlyMasterCanStart;

        [FormerlySerializedAs("startKey0")]
        public KeyCode StartKey0 = KeyCode.T;

        [FormerlySerializedAs("startKey1")]
        public KeyCode StartKey1 = KeyCode.E;

        [FormerlySerializedAs("startKey2")]
        public KeyCode StartKey2 = KeyCode.S;

        [FormerlySerializedAs("abortKey0")]
        [Header("Abort Input")]
        public KeyCode AbortKey0 = KeyCode.LeftControl;

        [FormerlySerializedAs("abortKey1")]
        public KeyCode AbortKey1 = KeyCode.C;

        [FormerlySerializedAs("StartOnPlayerJoin")]
        [FormerlySerializedAs("startOnPlayerJoin")]
        [Header("Tests")]
        public bool StartOnLocalPlayerJoin;

        public bool StartOnOtherPlayerJoin;

        [FormerlySerializedAs("tests")]
        public TestCase[] Tests;

        private bool _isTesting;
        private bool _testInitialized;
        private bool _testCompleted;
        private bool _testCleanedUp;

        private int _testIndex;
        private bool _pendingNextStep;

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog($"{nameof(OnPlayerJoined)} {player.ToStringSafe()}");
#endif

            #endregion


            if (OnlyMasterCanStart && !Networking.IsMaster)
            {
                Info($"{LogPrefix} {name}.OnPlayerJoined: Only master can start test");
                return;
            }

            if (StartOnLocalPlayerJoin && player.IsLocalSafe())
            {
                Info($"{LogPrefix} {name}.OnPlayerJoined: starting after local player {player.ToStringSafe()} joined");
                // start the tests
                _isTesting = true;
                ContinueTesting();
                return;
            }

            if (StartOnOtherPlayerJoin)
            {
                Info($"{LogPrefix} {name}.OnPlayerJoined: starting after other player {player.ToStringSafe()} joined");
                // start the tests
                _isTesting = true;
                ContinueTesting();
                return;
            }

            Info($"{LogPrefix} {name}.OnPlayerJoined: not started due to settings");
        }

        public void Update()
        {
            if (OnlyMasterCanStart && !Networking.IsMaster)
            {
                return;
            }

            if (_pendingNextStep)
            {
                _pendingNextStep = false;
                ContinueTesting();
            }

            if (_isTesting)
            {
                if (Input.GetKey(AbortKey0)
                    && Input.GetKey(AbortKey1))
                {
                    AbortTestRun();
                }
            }
            else
            {
                if (Input.GetKey(StartKey0)
                    && Input.GetKey(StartKey1)
                    && Input.GetKey(StartKey2))
                {
                    StartTestRun();
                }
            }
        }

        private void AbortTestRun()
        {
            Info($"{LogPrefix} {name}.AbortTestRun: aborting after current test");

            // set the index to the lenght of the list of tests to prevent continuation
            _testIndex = Tests.Length;
        }

        public void StartTestRun()
        {
            if (_isTesting)
            {
                Warn(
                    $"{LogPrefix} {name}.StartTestRun: can not start a new test while another " +
                    $"one is still running"
                );
                return;
            }

            Info($"{LogPrefix} {name}.StartTestRun");

            _isTesting = true;
            _testIndex = 0;
            _pendingNextStep = true;
        }

        private void ContinueTesting()
        {
            int numberOfTests = Tests.LengthSafe();
            if (numberOfTests > 0 && _testIndex > -1 && _testIndex < numberOfTests)
            {
                var testCase = Tests[_testIndex];
                if (!testCase)
                {
                    Error($"{LogPrefix} {name}.ContinueTesting: tests contains invalid behaviour");
                    return;
                }

                var context = testCase.gameObject;
                if (!_testInitialized)
                {
                    Info($"{LogPrefix} {name}.ContinueTesting: Initializing test {context}");
                    testCase.TestController = this;
                    testCase.Initialize();
                    return;
                }

                if (!_testCompleted)
                {
                    Info($"{LogPrefix} {name}.ContinueTesting: Running test {context}");
                    testCase.Run();
                    return;
                }

                if (!_testCleanedUp)
                {
                    Info($"{LogPrefix} {name}.ContinueTesting: Cleaning up test {context}");
                    testCase.CleanUp();
                    return;
                }

                ++_testIndex;
                _isTesting = _testIndex < Tests.Length;

                _testInitialized = false;
                _testCompleted = false;
                _testCleanedUp = false;

                _pendingNextStep = _isTesting;

                if (!_isTesting)
                {
                    Info($"{LogPrefix} {name}.ContinueTesting: All test completed");
                }
            }
            else
            {
                Warn($"{LogPrefix} {name}.ContinueTesting: test run aborted");
                _testIndex = 0;
                _isTesting = false;
                _testInitialized = false;
                _testCompleted = false;
                _testCleanedUp = false;
                _pendingNextStep = false;
            }
        }

        public void TestInitialized(bool success)
        {
            if (success)
            {
                Info($"{LogPrefix} {name}.TestInitialized: Initialized test successfully");
            }
            else
            {
                Error($"{LogPrefix} {name}.TestInitialized: Test initialization failed");
            }

            _testInitialized = true;
            if (!success)
            {
                _testCompleted = true;
            }

            _pendingNextStep = true;
        }

        public void TestCompleted(bool success)
        {
            if (success)
            {
                Info($"{LogPrefix} {name}.TestCompleted: PASS");
            }
            else
            {
                Error($"{LogPrefix} {name}.TestCompleted: FAIL");
            }

            _testCompleted = true;
            _pendingNextStep = true;
        }

        public void TestCleanedUp(bool success)
        {
            if (success)
            {
                Info($"{LogPrefix} {name}.TestCleanedUp: Cleaned up test successfully");
            }
            else
            {
                Error($"{LogPrefix} {name}.TestCleanedUp: Test cleanup failed");
            }

            // ensure we don't get any more messages from the test
            Tests[_testIndex].TestController = null;

            _testCleanedUp = true;
            _pendingNextStep = true;
        }
    }
}