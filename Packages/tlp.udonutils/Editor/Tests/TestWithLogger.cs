using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using NUnit.Framework;
using TLP.UdonUtils.Runtime.Logger;
using TLP.UdonUtils.Runtime.Sources.FrameCount;
using TLP.UdonUtils.Runtime.Sources.Time;
using TLP.UdonUtils.Runtime.Tests.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRC.SDKBase;
using Object = UnityEngine.Object;
using Assert = UnityEngine.Assertions.Assert;

namespace TLP.UdonUtils.Editor.Tests
{
    [TestFixture(Category = "TLP/UdonUtils")]
    public abstract class TestWithLogger
    {
        [PublicAPI]
        protected TlpLogger TlpLogger { get; private set; }

        protected UdonTestUtils.UdonTestEnvironment UdonTestEnvironment;
        protected VRCPlayerApi LocalPlayer;

        public void ClearLog() {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            Assert.IsNotNull(method, "Editor log clear method not found");
            method.Invoke(new object(), null);
        }

        [SetUp]
        public virtual void Setup() {
            ClearLog();
            Debug.ClearDeveloperConsole();
            Debug.Log("=========== Test Setup start ===========");

            DestroyGameObjects(SceneManager.GetActiveScene().GetRootGameObjects());

            UdonTestUtils.UdonTestEnvironment.ResetApiBindings();
            LogAssert.ignoreFailingMessages = false;
            TlpLogger = new GameObject(TlpLogger.ExpectedGameObjectName()).AddComponent<TlpLogger>();
            Debug.Log($"Created {TlpLogger.ExpectedGameObjectName()}: {TlpLogger == true}");
            TlpLogger.Severity = ELogLevel.Warning;
            TlpLogger.TimeSource = new GameObject(nameof(ConstantTime)).AddComponent<ConstantTime>();
            TlpLogger.FrameCount = new GameObject(nameof(ConstantFrameCount)).AddComponent<ConstantFrameCount>();
            TlpLogger.OnEnable();

            UdonTestEnvironment = new UdonTestUtils.UdonTestEnvironment();
            LocalPlayer = UdonTestEnvironment.CreatePlayer();
        }

        [TearDown]
        public virtual void CleanUp() {
            Debug.Log("=========== Test TearDown start ===========");
            LogAssert.ignoreFailingMessages = true;
            if (UdonTestEnvironment != null) {
                UdonTestEnvironment.Deconstruct();
                UdonTestEnvironment = null;
                LocalPlayer = null;
            }

            if (TlpLogger) {
                Object.DestroyImmediate(TlpLogger.gameObject);
                TlpLogger = null;
            }

            DestroyGameObjects(SceneManager.GetActiveScene().GetRootGameObjects());
        }

        protected static IEnumerator ShowAllGameObjects() {
            if (Application.isPlaying) {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) {
                    foreach (var componentInChild in root.GetComponentsInChildren<Transform>(true)) {
                        yield return null;
                        EditorGUIUtility.PingObject(componentInChild);
                    }
                }

                yield return new WaitForSeconds(10f);
            }
        }

        protected static void DestroyGameObjects(IEnumerable<GameObject> objectsToCleanup) {
            foreach (var objectToCleanUp in objectsToCleanup) {
                if (!objectToCleanUp || objectToCleanUp.name == "Code-based tests runner") {
                    continue;
                }

                Debug.Log($"Destroying {objectToCleanUp.name}");

                Object.DestroyImmediate(objectToCleanUp);
            }
        }

        protected static void ExpectError(string message) {
            LogAssert.Expect(LogType.Error, new Regex(".*" + message + ".*"));
        }

        protected static void ExpectAssert(string message) {
            ExpectError(message);
            LogAssert.Expect(LogType.Assert, new Regex(".*" + message + ".*"));
        }

        protected static void ExpectWarning(string message) {
            LogAssert.Expect(LogType.Warning, new Regex(".*" + message + ".*"));
        }

        protected static void ExpectLog(string message) {
            LogAssert.Expect(LogType.Log, new Regex(".*" + message + ".*"));
        }

        [PublicAPI]
        protected static GameObject FindGameObjectIncludingInactive(string goName) {
            GameObject gameObject = null;
            foreach (var rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects()) {
                foreach (var transform in rootGameObject.GetComponentsInChildren<Transform>(true)) {
                    if (transform.gameObject.name.Equals(goName, StringComparison.InvariantCultureIgnoreCase)) {
                        gameObject = transform.gameObject;
                    }
                }
            }

            return gameObject;
        }

        public void AreApproximatelyEqual(Vector3 a, Vector3 b, float delta = 1e-5f) {
            bool ok = false;
            try {
                Assert.AreApproximatelyEqual(a.x, b.x, delta);
                Assert.AreApproximatelyEqual(a.y, b.y, delta);
                Assert.AreApproximatelyEqual(a.z, b.z, delta);
                ok = true;
            }
            finally {
                if (!ok) Debug.LogError($"{a} != {b}");
            }
        }

        /// <summary>
        /// Yields null in edit mode test until the give amount of time has elapsed (realtime)
        /// </summary>
        /// <code>
        /// // Example usage:
        /// yield return EditorTestWaitForSeconds(2);
        /// </code>
        /// <param name="duration"></param>
        /// <returns>yield return null</returns>
        public static IEnumerator EditorTestWaitForSeconds(float duration) {
            Debug.Assert(!Application.isPlaying, "Use WaitForSeconds instead in PlaymodeTests");
            float time = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - time < duration) {
                yield return null;
            }
        }
    }
}