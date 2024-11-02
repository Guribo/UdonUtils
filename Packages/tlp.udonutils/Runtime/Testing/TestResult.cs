using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Testing;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace TLP.UdonUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestResult), ExecutionOrder)]
    public class TestResult : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestData.ExecutionOrder + 1;

        [SerializeField]
        private TextMeshProUGUI Id;

        [SerializeField]
        private TextMeshProUGUI Title;

        [SerializeField]
        private TextMeshProUGUI ButtonText;

        [SerializeField]
        private TextMeshProUGUI Status;

        [SerializeField]
        private Image Background;

        public void Initialize(TestCase testCase) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Initialize));
#endif
            #endregion

            Id.text = testCase.transform.GetSiblingIndex().ToString();
            Title.text = testCase.name;

            ButtonText.text = "Start";
            Background.color = Color.gray;
            switch (testCase.Status) {
                case TestCaseStatus.Ready:
                    Status.text = "Ready";
                    break;
                case TestCaseStatus.Running:
                    Status.text = "Running";
                    Background.color = Color.blue;
                    break;
                case TestCaseStatus.Passed:
                    Status.text = "Pass";
                    Background.color = Color.green;
                    break;
                case TestCaseStatus.Failed:
                    Status.text = "Fail";
                    Background.color = Color.red;
                    break;
                case TestCaseStatus.NotRun:
                    Status.text = "-";
                    Background.color = Color.yellow;
                    break;
                default:
                    Status.text = "Unknown";
                    Background.color = Color.magenta;
                    break;
            }


            testCase.Result = this;
            name = $"TableEntry_{testCase.name}";
        }
    }
}