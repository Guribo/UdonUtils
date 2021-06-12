﻿using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonUtils.Scripts.Testing
{
    /// <summary>
    /// Component which implements the base of a test case, includes preparation, execution and cleanup methods
    /// to be copied to new test scripts and filled for each individual test case.
    /// 
    /// Behaviour sync mode can be changed depending on the test performed, default is no variable sync
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class TestTemplate : UdonSharpBehaviour
    {
        #region DO NOT EDIT

        [NonSerialized] public TestController testController;

        public void Initialize()
        {
            if (!testController)
            {
                Debug.LogError("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize: invalid test controller", this);
                return;
            }
            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize", this);
            InitializeTest();
        }

        public void Run()
        {
            if (!testController)
            {
                Debug.LogError("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run: invalid test controller", this);
                return;
            }

            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run", this);
            RunTest();
        }

        public void CleanUp()
        {
            if (!testController)
            {
                Debug.LogError("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp: invalid test controller", this);
                return;
            }
            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp", this);
            CleanUpTest();
        }

        #endregion

        #region EDIT HERE

        private void InitializeTest()
        {
            // TODO your init behaviour here
            // ...

            // whenever the test is ready to be started call TestController.TestInitialized,
            // can be later in update or whenever but MUST be called at some point
            testController.TestInitialized(true);
        }

        private void RunTest()
        {
            // TODO your test behaviour here
            // ...

            // whenever the test is completed call TestController.TestCompleted,
            // can be later in update or whenever but MUST be called at some point
            testController.TestCompleted(true);
        }

        private void CleanUpTest()
        {
            // TODO your clean up behaviour here
            // ...

            // whenever the test is cleaned up call TestController.TestCleanedUp,
            // can be later in update or whenever but MUST be called at some point
            testController.TestCleanedUp(true);
        }
        
        private bool Assert(bool condition, string message)
        {
            if (!condition)
            {
                if (Utilities.IsValid(this))
                {
                    Debug.LogError("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Assertion failed : '" + GetType() + " : " + message + "'", this);
                }
                else
                {
                    Debug.LogError("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Assertion failed :  'UNKNOWN TYPE: " + message + "'");
                }

                return false;
            }

            Debug.Assert(condition, message);
            return true;
        }

        #endregion
    }
}
