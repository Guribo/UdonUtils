using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Experimental.Tasks
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ExampleTask), ExecutionOrder)]
    public class ExampleTask : Task
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = Task.ExecutionOrder + 1;
        #endregion

        private int _steps;

        private void OnEnable() {
            TaskScheduler._AddTaskToDefaultScheduler(this, this);
        }

        private const string ExtraWork = "Extra Work";

        protected override TaskResult _DoTask(float stepDeltaTime) {
            #region TLP_DEBUG
#if TLP_DEBUG
        _DebugLog($"{nameof(_DoTask)}: {nameof(stepDeltaTime)}={stepDeltaTime}");
#endif
#endregion
            if (_steps >= 100) return TaskResult.Succeeded;

            var iterations = Random.Range(0, 10);


            // for (int i = 0; i < iterations; i++) {
            //     Info(ExtraWork);
            // }

            _Info($"Step {_steps++}");
            _SetProgress(_steps / 100f);
            return TaskResult.Unknown;
        }

        public override int _GetNeededSteps() {
            return 100;
        }

        protected override bool _InitTask() {
            _steps = 0;
            return true;
        }

        public override void _OnEvent(string eventName) {
            switch (eventName) {
                case nameof(_OnTaskFinished):
                    _OnTaskFinished();
                    break;
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }

        protected override void _OnTaskFinished() {
            TaskScheduler._AddTaskToDefaultScheduler(this, this);
        }
    }
}