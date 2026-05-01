using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HarmonyLib;
using JetBrains.Annotations;
using NSubstitute;
using NUnit.Framework;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.EditorOnly;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Logger;
using TLP.UdonUtils.Runtime.Sources;
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
        protected ConstantTime GameTime, RealNetworkTime;
        protected ConstantFrameCount FrameCount;

        /// <summary>
        /// Create on-demand with <see cref="CreateTlpNetworkTime"/>
        /// </summary>
        protected TlpNetworkTime NetworkTime;

        [SetUp]
        public virtual void Setup() {
            LogAssert.ignoreFailingMessages = false;

            ClearLog();
            Debug.ClearDeveloperConsole();

            DestroyGameObjects(SceneManager.GetActiveScene().GetRootGameObjects());
            PrepareUdonTestEnvironmentWithLocalPlayer();
            CreateAndActivateLogger();
        }

        [TearDown]
        public virtual void CleanUp() {
            Debug.Log("=========== Test TearDown start ===========");
            LogAssert.ignoreFailingMessages = true;
            ResetTestEnvironment();
            DestroyLoggerInstance();
            DestroyGameObjects(SceneManager.GetActiveScene().GetRootGameObjects());
        }
        
        [Test]
        public void SanityTest() {
            // Arrange
            var mock = Substitute.For<TlpBaseBehaviour>();

            // Act
            mock.OnEvent("Foo");

            // Assert
            mock.Received().OnEvent("Foo");
        }
        
        public static void ClearLog() {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            Assert.IsNotNull(method, "Editor log clear method not found");
            method.Invoke(new object(), null);
        }

        [PublicAPI]
        protected static IEnumerator ShowAllGameObjects() {
            if (!Application.isPlaying) {
                yield break;
            }

            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) {
                foreach (var componentInChild in root.GetComponentsInChildren<Transform>(true)) {
                    yield return null;
                    EditorGUIUtility.PingObject(componentInChild);
                }
            }

            yield return new WaitForSeconds(10f);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AreApproximatelyEqual(Vector3 a, Vector3 b, float delta = 1e-5f, string message = "")
        {
            Assert.IsFalse(Mathf.Abs(a.x - b.x) > delta ||
                           Mathf.Abs(a.y - b.y) > delta ||
                           Mathf.Abs(a.z - b.z) > delta, $" {a} != {b} " + message);
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

        [PublicAPI]
        protected void CreateTlpNetworkTime() {
            NetworkTime = new GameObject("TLP_NetworkTime").AddComponent<TlpNetworkTime>();
            GameTime = new GameObject("TLP_GameTime").AddComponent<ConstantTime>();
            RealNetworkTime = new GameObject("TLP_RealNetworkTime").AddComponent<ConstantTime>();
            FrameCount = new GameObject("TLP_FrameCount").AddComponent<ConstantFrameCount>();
            NetworkTime.RealNetworkTime = RealNetworkTime;
            NetworkTime.GameTime = GameTime;
            NetworkTime.FrameCount = FrameCount;
            NetworkTime.OnReferenceTimeUpdated = new GameObject("OnReferenceTimeUpdated").AddComponent<UdonEvent>();
            NetworkTime.Start();
        }
        
        #region Utilties.IsValid Patch
        
     
        protected Harmony _harmony;
        
        /// <summary>
        /// Patch Utilities.IsValid to only check for null, thus accepting non-Unity objects as valid.
        /// Useful for testing with NSubstitute mocks.
        /// </summary>
        protected void SetupUtilitiesIsValidPatch() {
            _harmony = new Harmony("tlp.tests");
            _harmony.Patch(
                    original: AccessTools.Method(typeof(Utilities), nameof(Utilities.IsValid)),
                    prefix: new HarmonyMethod(typeof(TestWithLogger), nameof(IsValidPatch))
            );
        }
        
        protected void CleanupUtilitiesIsValidPatch() {
            _harmony.UnpatchAll("tlp.tests");
        }
        // ReSharper disable once RedundantAssignment
        private static bool IsValidPatch(object obj, ref bool __result)
        {
            if (obj == null) { __result = false; return false; }
            __result = true;
            return false; // skip original
        }
        #endregion

        #region Hooks
        /// <summary>
        /// Hook to provide the initial <see cref="TimeSource"/> for the logger.
        /// Defaults to a <see cref="ConstantTime"/> component on a new <see cref="GameObject"/>.
        /// </summary>
        /// <returns>A new <see cref="TimeSource"/> instance.</returns>
        protected virtual TimeSource GetInitialLoggerTimeSource() {
            return new GameObject(nameof(ConstantTime)).AddComponent<ConstantTime>();
        }

        /// <summary>
        /// Hook to provide the initial <see cref="FrameCountSource"/> for the logger.
        /// Defaults to a <see cref="ConstantFrameCount"/> component on a new <see cref="GameObject"/>.
        /// </summary>
        /// <returns>A new <see cref="FrameCountSource"/> instance.</returns>
        protected virtual FrameCountSource GetInitialLoggerFrameCountSource() {
            return new GameObject(nameof(ConstantFrameCount)).AddComponent<ConstantFrameCount>();
        }
        #endregion

        #region Internal
        private void CreateAndActivateLogger() {
            var tlpLoggerObject = new GameObject(TlpLogger.ExpectedGameObjectName());
            tlpLoggerObject.SetActive(false);
            TlpLogger = tlpLoggerObject.AddComponent<TlpLogger>();
            TlpLogger.Severity = ELogLevel.Warning;
            TlpLogger.TimeSource = GetInitialLoggerTimeSource();
            TlpLogger.FrameCount = GetInitialLoggerFrameCountSource();
            tlpLoggerObject.SetActive(true);

            if (!Application.isPlaying) {
                TlpLogger.OnEnable();
            }
        }

        private void PrepareUdonTestEnvironmentWithLocalPlayer() {
            UdonTestUtils.UdonTestEnvironment.ResetApiBindings();
            UdonTestEnvironment = new UdonTestUtils.UdonTestEnvironment();
            LocalPlayer = UdonTestEnvironment.CreatePlayer();
        }

        private void DestroyLoggerInstance() {
            if (!TlpLogger) {
                return;
            }

            Object.DestroyImmediate(TlpLogger.gameObject);
            TlpLogger = null;
        }

        private void ResetTestEnvironment() {
            if (UdonTestEnvironment == null) {
                return;
            }

            UdonTestEnvironment.Deconstruct();
            UdonTestEnvironment = null;
            LocalPlayer = null;
        }
        #endregion
    }
}