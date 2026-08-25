using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace TLP.UdonUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestResultsUi), ExecutionOrder)]
    public class TestResultsUi : View
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = DummyView.ExecutionOrder + 1;

        [SerializeField]
        internal TestResult Prefab;

        [SerializeField]
        private RectTransform ContentPanel;

        private readonly DataDictionary _testEntries = new DataDictionary();

        public override void _OnModelChanged() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnModelChanged));
#endif
            #endregion

            var testData = (TestData)Model;
            int tests = testData.Tests.Count;

            float rectHeight = ((RectTransform)Prefab.transform).rect.height;
            ContentPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectHeight * tests);

            for (int i = 0; i < tests; i++) {
                var testCase = (TestCase)testData.Tests[i].Reference;
                if (_testEntries.ContainsKey(testCase)) {
                    ((TestResult)_testEntries[testCase].Reference)._Initialize(testCase);
                    continue;
                }

                var newEntry = Instantiate(Prefab.gameObject, ContentPanel);
                var newTestResult = newEntry.transform.GetComponent<TestResult>();
                _testEntries.Add(testCase, newTestResult);

                newTestResult._Initialize(testCase);
                var newEntryTransform = newTestResult.transform;
                var transformLocalPosition = newEntryTransform.localPosition;
                newEntryTransform.localPosition = new Vector3(
                        transformLocalPosition.x,
                        -i * rectHeight,
                        transformLocalPosition.z
                );
                newTestResult.gameObject.SetActive(true);
                _DebugLog("Added test result entry");
            }
        }

        protected override bool _InitializeInternal() {
            if (!Prefab) {
                _Error($"{nameof(Prefab)} not set");
                return false;
            }

            Prefab.gameObject.SetActive(false);

            return base._InitializeInternal();
        }
    }
}