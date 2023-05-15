using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using NUnit.Framework;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Logger;
using TLP.UdonUtils.Tests.Runtime.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP")]
    public abstract class TestWithLogger
    {
        private TlpLogger _tlpLogger;

        [PublicAPI]
        protected TlpLogger TlpLogger => _tlpLogger;

        protected UdonTestUtils.UdonTestEnvironment UdonTestEnvironment;
        protected VRCPlayerApi LocalPlayer;

        public void ClearLog()
        {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            if (method == null)
            {
                Assert.Fail("Editor log clear method not found");
            }

            method.Invoke(new object(), null);
        }

        [SetUp]
        public virtual void Setup()
        {
            ClearLog();
            Debug.ClearDeveloperConsole();
            Debug.Log("=========== Test Setup start ===========");

            DestroyGameObjects(SceneManager.GetActiveScene().GetRootGameObjects());

            UdonTestUtils.UdonTestEnvironment.ResetApiBindings();
            LogAssert.ignoreFailingMessages = false;
            _tlpLogger = new GameObject(TlpBaseBehaviour.TlpLoggerGameObjectName).AddComponent<TlpLogger>();
            Debug.Log($"Created {TlpBaseBehaviour.TlpLoggerGameObjectName}: {_tlpLogger == true}");
            _tlpLogger.Severity = ELogLevel.Warning;

            UdonTestEnvironment = new UdonTestUtils.UdonTestEnvironment();
            LocalPlayer = UdonTestEnvironment.CreatePlayer();
        }

        [TearDown]
        public virtual void CleanUp()
        {
            Debug.Log("=========== Test TearDown start ===========");
            LogAssert.ignoreFailingMessages = true;
            if (UdonTestEnvironment != null)
            {
                UdonTestEnvironment.Deconstruct();
                UdonTestEnvironment = null;
                LocalPlayer = null;
            }

            if (_tlpLogger)
            {
                Object.DestroyImmediate(_tlpLogger.gameObject);
                _tlpLogger = null;
            }

            DestroyGameObjects(SceneManager.GetActiveScene().GetRootGameObjects());
        }

        protected static IEnumerator ShowAllGameObjects()
        {
            if (Application.isPlaying)
            {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    foreach (var componentInChild in root.GetComponentsInChildren<Transform>(true))
                    {
                        yield return null;
                        EditorGUIUtility.PingObject(componentInChild);
                    }
                }

                yield return new WaitForSeconds(10f);
            }
        }

        protected static void DestroyGameObjects(IEnumerable<GameObject> objectsToCleanup)
        {
            foreach (var objectToCleanUp in objectsToCleanup)
            {
                if (!objectToCleanUp || objectToCleanUp.name == "Code-based tests runner")
                {
                    continue;
                }

                Debug.Log($"Destroying {objectToCleanUp.name}");

                Object.DestroyImmediate(objectToCleanUp);
            }
        }

        protected static void ExpectError(string message)
        {
            LogAssert.Expect(LogType.Error, new Regex(".*" + message + ".*"));
        }

        protected static void ExpectWarning(string message)
        {
            LogAssert.Expect(LogType.Warning, new Regex(".*" + message + ".*"));
        }

        protected static void ExpectLog(string message)
        {
            LogAssert.Expect(LogType.Log, new Regex(".*" + message + ".*"));
        }

        protected static GameObject FindGameObjectIncludingInactive(string goName)
        {
            GameObject gameObject = null;
            foreach (var rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var transform in rootGameObject.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.gameObject.name.Equals(goName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        gameObject = transform.gameObject;
                    }
                }
            }


            return gameObject;
        }
    }
}