using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonUtils.Scripts.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class TestController : UdonSharpBehaviour
    {
        private const string LogPrefix = "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>]";

        [Header("Start Input")]
        public KeyCode startKey0 = KeyCode.T;
        public KeyCode startKey1 = KeyCode.E;
        public KeyCode startKey2 = KeyCode.S;

        [Header("Abort Input")]
        public KeyCode abortKey0 = KeyCode.LeftControl;
        public KeyCode abortKey1 = KeyCode.C;

        [Header("Tests")]
        public bool startOnPlayerJoin;

        public UdonSharpBehaviour[] tests;

        private bool _isTesting;
        private bool _testInitialized;
        private bool _testCompleted;
        private bool _testCleanedUp;

        private int _testIndex;
        private bool _pendingNextStep;

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!startOnPlayerJoin)
            {
                return;
            }

            if (Networking.LocalPlayer == player)
            {
                // start the tests
                _isTesting = true;
                ContinueTesting();
            }
        }

        public void Update()
        {
            if (_pendingNextStep)
            {
                _pendingNextStep = false;
                ContinueTesting();
            }

            if (_isTesting)
            {
                if (Input.GetKey(abortKey0)
                    && Input.GetKey(abortKey1))
                {
                    AbortTestRun();
                }
            }
            else
            {
                if (Input.GetKey(startKey0)
                    && Input.GetKey(startKey1)
                    && Input.GetKey(startKey2))
                {
                    StartTestRun();
                }
            }
        }

        private void AbortTestRun()
        {
            Debug.Log($"{LogPrefix} {name}.AbortTestRun: aborting after current test",
                this);

            // set the index to the lenght of the list of tests to prevent continuation
            _testIndex = tests.Length;
        }

        public void StartTestRun()
        {
            if (_isTesting)
            {
                Debug.LogWarning($"{LogPrefix} {name}.StartTestRun: can not start a new test while another " +
                                 $"one is still running",
                    this);
                return;
            }

            Debug.Log($"{LogPrefix} {name}.StartTestRun", this);

            _isTesting = true;
            _testIndex = 0;
            _pendingNextStep = true;
        }

        private void ContinueTesting()
        {
            if (tests != null && tests.Length > 0 && _testIndex > -1 && _testIndex < tests.Length)
            {
                var udonSharpBehaviour = tests[_testIndex];
                if (!udonSharpBehaviour)
                {
                    Debug.LogError($"{LogPrefix} {name}.ContinueTesting: tests contains invalid behaviour", this);
                    return;
                }

                var context = udonSharpBehaviour.gameObject;
                if (!_testInitialized)
                {
                    Debug.Log($"{LogPrefix} {name}.ContinueTesting: Initializing test {context}", context);
                    udonSharpBehaviour.SetProgramVariable("testController", this);
                    udonSharpBehaviour.SendCustomEvent("Initialize");
                    return;
                }

                if (!_testCompleted)
                {
                    Debug.Log($"{LogPrefix} {name}.ContinueTesting: Running test {context}", context);
                    udonSharpBehaviour.SendCustomEvent("Run");
                    return;
                }

                if (!_testCleanedUp)
                {
                    Debug.Log($"{LogPrefix} {name}.ContinueTesting: Cleaning up test {context}", context);
                    udonSharpBehaviour.SendCustomEvent("CleanUp");
                    return;
                }

                ++_testIndex;
                _isTesting = _testIndex < tests.Length;

                _testInitialized = false;
                _testCompleted = false;
                _testCleanedUp = false;

                _pendingNextStep = _isTesting;

                if (!_isTesting)
                {
                    Debug.Log($"{LogPrefix} {name}.ContinueTesting: All test completed");
                }
            }
            else
            {
                Debug.LogError($"{LogPrefix} {name}.ContinueTesting: test run aborted");
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
            Debug.Log($"{LogPrefix} {name}.TestInitialized: Initialized test successfully: {success}", this);
            _testInitialized = true;
            if (!success)
            {
                _testCompleted = true;
            }

            _pendingNextStep = true;
        }

        public void TestCompleted(bool success)
        {
            Debug.Log($"{LogPrefix} {name}.TestCompleted: Test ran successfully {success}", this);
            _testCompleted = true;
            _pendingNextStep = true;
        }

        public void TestCleanedUp(bool success)
        {
            Debug.Log($"{LogPrefix} {name}.TestCleanedUp: Cleaned up test successfully: {success}", this);
            _testCleanedUp = true;
            _pendingNextStep = true;
        }
    }
}