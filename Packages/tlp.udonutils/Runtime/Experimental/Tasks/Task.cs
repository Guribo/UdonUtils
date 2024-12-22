using JetBrains.Annotations;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Experimental.Tasks
{
    public enum TaskState
    {
        Finished,
        Pending,
        Running
    }

    public enum TaskResult
    {
        Unknown,
        Succeeded,
        Failed,
        Aborted
    }

    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(Task), ExecutionOrder)]
    public abstract class Task : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = Runtime.Pool.Pool.ExecutionOrder + 50;
        #endregion

        public TaskState State { get; private set; } = TaskState.Finished;
        public TaskResult Result { get; private set; } = TaskResult.Unknown;

        internal TaskScheduler DefaultScheduler;
        internal TaskScheduler ActiveScheduler;
        internal TlpBaseBehaviour TaskInstigator;

        /// <summary>
        /// In range 0 - 1 (inclusive)
        /// </summary>
        public float Progress { get; private set; }

        internal bool PrepareForRun() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(PrepareForRun));
#endif
            #endregion

            State = TaskState.Pending;
            Progress = 0f;
            Result = TaskResult.Unknown;
            if (InitTask()) {
                return true;
            }

            Result = TaskResult.Failed;
            State = TaskState.Finished;
            return false;
        }

        public bool Abort() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Abort));
#endif
            #endregion

            if (State == TaskState.Finished) {
                Error("Task is already completed");
                return false;
            }

            State = TaskState.Finished;
            Result = TaskResult.Aborted;

            if (Utilities.IsValid(ActiveScheduler)) {
                ActiveScheduler.CancelTask(this);
                return true;
            }

            Error($"{nameof(Abort)}: was not scheduled");
            return false;
        }

        public TaskState Run() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Run));
#endif
            #endregion

            if (Result == TaskResult.Aborted) {
                Warn("Task is already aborted");
                State = TaskState.Finished;
                return State;
            }

            switch (State) {
                case TaskState.Pending:
                    State = TaskState.Running;
                    RunNextStep();
                    break;
                case TaskState.Running:
                    RunNextStep();
                    break;
                case TaskState.Finished:
                    Error("Task is already completed");
                    break;
                default:
                    Error($"Unknown {nameof(TaskState)}: {State}");
                    State = TaskState.Finished;
                    Result = TaskResult.Aborted;
                    break;
            }

            return State;
        }

        #region Internal
        protected void SetProgress(float progress) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(SetProgress)}: {nameof(progress)}={progress}");
#endif
            #endregion


            Progress = Mathf.Clamp01(progress);
        }

        private void RunNextStep() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RunNextStep));
#endif
            #endregion

            switch (RunStep()) {
                case TaskResult.Unknown:
                    Result = TaskResult.Unknown;
                    break;
                case TaskResult.Succeeded:
                    SetProgress(1);
                    Result = TaskResult.Succeeded;
                    State = TaskState.Finished;
                    break;
                case TaskResult.Failed:
                    Result = TaskResult.Failed;
                    State = TaskState.Finished;
                    break;
                case TaskResult.Aborted:
                    Result = TaskResult.Aborted;
                    State = TaskState.Finished;
                    break;
                default:
                    Error($"Unknown {nameof(TaskState)}: {State}");
                    State = TaskState.Finished;
                    Result = TaskResult.Aborted;
                    break;
            }
        }
        #endregion


        /// <summary>
        /// Will only ever be called when the Task is in the <see cref="TaskState.Running"/> state.
        /// Do as much work as needed, but try stay within the <see cref="TaskScheduler.MinTimeBudget"/>.
        /// Keep track of your work and <see cref="Progress"/> and ensure to return the new <see cref="TaskState"/>
        /// afterwards.
        /// </summary>
        /// <returns>
        /// Shall return <see cref="TaskResult.Unknown"/> while not finished completely.
        /// Shall return <see cref="TaskResult.Aborted"/> if task result of the task is not needed anymore.
        /// Shall return <see cref="TaskResult.Failed"/> if task failed to produce a result.
        /// Shall return <see cref="TaskResult.Succeeded"/> if task produced the desired result.
        /// </returns>
        protected abstract TaskResult RunStep();

        protected abstract bool InitTask();

        #region Public API
        public bool TryScheduleTask(TlpBaseBehaviour instigator) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(TryScheduleTask));
#endif
            #endregion

            if (State != TaskState.Finished) {
                return true;
            }

            if (TaskScheduler.AddTaskToDefaultScheduler(instigator, this)) {
                return true;
            }

            Error($"{nameof(TryScheduleTask)}: Can't add task to default scheduler");
            return false;
        }
        #endregion
    }
}