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

        #region Dependencies
        [Header("Task")]
        [Tooltip("Optional, when empty the scene is searched for a 'TLP_TaskScheduler' GameObject")]
        public TaskScheduler DefaultScheduler;

        internal TaskScheduler ActiveScheduler;
        internal TlpBaseBehaviour TaskInstigator;
        #endregion

        #region Internal

        internal float EstimatedStepDuration { get; private set; }
        internal void UpdateEstimatedStepDuration(float stepDuration) {
            EstimatedStepDuration = Mathf.Lerp(EstimatedStepDuration, 0, 0.01f);
            EstimatedStepDuration = Mathf.Max(EstimatedStepDuration, stepDuration);
        }
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

        #region Protected

        protected void SetProgress(float progress) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(SetProgress)}: {nameof(progress)}={progress}");
#endif
            #endregion

            Progress = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// Called with:
        ///    State = <see cref="TaskState.Pending"/>;
        ///    Progress = 0;
        ///    Result = <see cref="TaskResult.Unknown"/>;
        /// </summary>
        /// <returns>true when init succeeded</returns>
        protected virtual bool InitTask() {
            #region TLP_DEBUG
#if TLP_DEBUG
        DebugLog(nameof(InitTask));
#endif
            #endregion

            return true;
        }

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

        /// <summary>
        /// Called on finished/aborted tasks with:
        ///    State = <see cref="TaskState.Finished"/>;
        ///    Progress = 0;
        ///    Result =  <see cref="TaskResult.Succeeded"/>/<see cref="TaskResult.Failed"/>/<see cref="TaskResult.Aborted"/>;
        /// <remarks>Called after instigator is notified about task result, but only if <see cref="TaskState"/> != <see cref="TaskState.Finished"/>;.</remarks>
        /// </summary>
        /// <returns>false to indicated something went wrong</returns>
        public virtual bool DeInitTask() {
            #region TLP_DEBUG
#if TLP_DEBUG
        DebugLog(nameof(DeInitTask));
#endif
            #endregion

            return true;
        }
        #endregion

        #region Public API
        public TaskState State { get; private set; } = TaskState.Finished;
        public TaskResult Result { get; private set; } = TaskResult.Unknown;

        /// <summary>
        /// In range 0 - 1 (inclusive)
        /// </summary>
        public float Progress { get; private set; }

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

        public virtual int GetNeededSteps() {
            return 1;
        }
        #endregion
    }
}