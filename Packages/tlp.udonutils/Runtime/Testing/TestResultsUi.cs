using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TestResultsUi : View
{
    [SerializeField]
    internal TestResult Prefab;

    [SerializeField]
    private RectTransform ContentPanel;

    private DataDictionary _testEntries = new DataDictionary();

    public override void OnModelChanged() {
        #region TLP_DEBUG
#if TLP_DEBUG
        DebugLog(nameof(OnModelChanged));
#endif
        #endregion

        var testData = (TestData)Model;
        int tests = testData.Tests.Count;

        float rectHeight = ((RectTransform)Prefab.transform).rect.height;
        ContentPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectHeight * tests);

        for (int i = 0; i < tests; i++) {
            var testCase = (TestCase)testData.Tests[i].Reference;
            if (_testEntries.ContainsKey(testCase)) {
                ((TestResult)_testEntries[testCase].Reference).Initialize(testCase);
                continue;
            }

            var newEntry = Instantiate(Prefab.gameObject, ContentPanel);
            var newTestResult = newEntry.transform.GetComponent<TestResult>();
            _testEntries.Add(testCase, newTestResult);

            newTestResult.Initialize(testCase);
            var newEntryTransform = newTestResult.transform;
            var transformLocalPosition = newEntryTransform.localPosition;
            newEntryTransform.localPosition = new Vector3(
                    transformLocalPosition.x,
                    -i * rectHeight,
                    transformLocalPosition.z
            );
            newTestResult.gameObject.SetActive(true);
            DebugLog("Added test result entry");
        }
    }

    protected override bool InitializeInternal() {
        if (!Prefab) {
            Error($"{nameof(Prefab)} not set");
            return false;
        }

        Prefab.gameObject.SetActive(false);

        return base.InitializeInternal();
    }
}