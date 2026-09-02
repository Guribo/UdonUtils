using System;
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
        Blocked,
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

        #region Constants
        public const string FinishedTaskCallbackName = nameof(_OnTaskFinished);
        #endregion

        #region Dependencies
        [Header("Task")]
        [Tooltip("Optional, when empty the scene is searched for a 'TLP_TaskScheduler' GameObject")]
        public TaskScheduler DefaultScheduler;

        public bool LimitToOncePerFrame;

        internal TaskScheduler ActiveScheduler;
        internal TlpBaseBehaviour TaskInstigator;
        #endregion

        #region Internal
        internal float EstimatedStepDuration { get; private set; }

        private int FrameOfLastStep { get; set; }
        private double TimeOfLastStep { get; set; }
        private float StepDeltaTime { get; set; }

        internal void _UpdateEstimatedStepDuration(float stepDuration) {
            EstimatedStepDuration = Mathf.Lerp(EstimatedStepDuration, stepDuration, 0.1f);
            EstimatedStepDuration = Mathf.Max(EstimatedStepDuration, stepDuration);
        }

        internal bool _PrepareForRun() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_PrepareForRun));
#endif
            #endregion

            State = TaskState.Pending;
            Progress = 0f;
            Result = TaskResult.Unknown;
            TimeOfLastStep = Time.unscaledTimeAsDouble;
            StepDeltaTime = LimitToOncePerFrame ? Time.unscaledDeltaTime : 0f;
            if (_InitTask()) {
                return true;
            }

            Result = TaskResult.Failed;
            State = TaskState.Finished;
            return false;
        }

        private void _RunNextStep() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RunNextStep));
#endif
            #endregion

            if (LimitToOncePerFrame) {
                if (Time.frameCount == FrameOfLastStep) {
                    #region TLP_DEBUG
#if TLP_DEBUG
                    _DebugLog($"{nameof(_RunNextStep)}: Already ran this frame");
#endif
                    #endregion

                    Result = TaskResult.Blocked;
                    return;
                }

                StepDeltaTime = Mathf.Max(Time.unscaledDeltaTime, (float)(Time.unscaledTimeAsDouble - TimeOfLastStep));
            } else {
                StepDeltaTime = (float)(Time.unscaledTimeAsDouble - TimeOfLastStep);
            }

            FrameOfLastStep = Time.frameCount;

            switch (_DoTask(StepDeltaTime)) {
                case TaskResult.Unknown:
                    Result = TaskResult.Unknown;
                    break;
                case TaskResult.Blocked:
                    Result = TaskResult.Blocked;
                    break;
                case TaskResult.Succeeded:
                    _SetProgress(1);
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
                    _Error($"Unknown {nameof(TaskState)}: {State}");
                    State = TaskState.Finished;
                    Result = TaskResult.Aborted;
                    break;
            }

            TimeOfLastStep = Time.unscaledTimeAsDouble;
        }
        #endregion

        #region Protected
        protected void _SetProgress(float progress) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_SetProgress)}: {nameof(progress)}={progress}");
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
        protected virtual bool _InitTask() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_InitTask));
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
        /// <param name="stepDeltaTime">unscaled delta time (game time) since the last step,
        /// can be 0 when <see cref="LimitToOncePerFrame"/> is false and the task runs multiple
        /// times in the same frame</param>
        /// <returns>
        /// Shall return <see cref="TaskResult.Unknown"/> while not finished completely.
        /// Shall return <see cref="TaskResult.Aborted"/> if task result of the task is not needed anymore.
        /// Shall return <see cref="TaskResult.Failed"/> if task failed to produce a result.
        /// Shall return <see cref="TaskResult.Succeeded"/> if task produced the desired result.
        /// </returns>
        protected virtual TaskResult _DoTask(float stepDeltaTime) {
            return TaskResult.Succeeded;
        }

        /// <summary>
        /// Called on finished/aborted tasks with:
        ///    State = <see cref="TaskState.Finished"/>;
        ///    Progress = 0;
        ///    Result =  <see cref="TaskResult.Succeeded"/>/<see cref="TaskResult.Failed"/>/<see cref="TaskResult.Aborted"/>;
        /// <remarks>Called after instigator is notified about task result, but only if <see cref="TaskState"/> != <see cref="TaskState.Finished"/>;.</remarks>
        /// </summary>
        /// <returns>false to indicated something went wrong</returns>
        public virtual bool _DeInitTask() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_DeInitTask));
#endif
            #endregion

            return true;
        }

        /// <summary>
        /// Do not schedule this task again in this method. Do so in <see cref="_DeInitTask"/> instead.
        /// </summary>
        protected virtual void _OnTaskFinished() {
        }
        #endregion

        #region Public API
        public TaskState State { get; private set; } = TaskState.Finished;
        public TaskResult Result { get; private set; } = TaskResult.Unknown;

        /// <summary>
        /// In range 0 - 1 (inclusive)
        /// </summary>
        public float Progress { get; private set; }

        public bool _TryScheduleTask(TlpBaseBehaviour instigator) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_TryScheduleTask));
#endif
            #endregion

            if (State != TaskState.Finished) {
                return true;
            }

            if (TaskScheduler._AddTaskToDefaultScheduler(instigator, this)) {
                return true;
            }

            _Error($"{nameof(_TryScheduleTask)}: Can't add task to default scheduler");
            return false;
        }

        public TaskState _Run() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Run));
#endif
            #endregion

            if (Result == TaskResult.Aborted) {
                _Warn("Task is already aborted");
                State = TaskState.Finished;
                return State;
            }

            switch (State) {
                case TaskState.Pending:
                    State = TaskState.Running;
                    _RunNextStep();
                    break;
                case TaskState.Running:

                    _RunNextStep();
                    break;
                case TaskState.Finished:
                    _Error("Task is already completed");
                    break;
                default:
                    _Error($"Unknown {nameof(TaskState)}: {State}");
                    State = TaskState.Finished;
                    Result = TaskResult.Aborted;
                    break;
            }

            return State;
        }


        public bool _Abort() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Abort));
#endif
            #endregion

            if (State == TaskState.Finished) {
                _Error("Task is already completed");
                return false;
            }

            State = TaskState.Finished;
            Result = TaskResult.Aborted;

            if (Utilities.IsValid(ActiveScheduler)) {
                ActiveScheduler._CancelTask(this);
                return true;
            }

            _Error($"{nameof(_Abort)}: was not scheduled");
            return false;
        }

        public virtual int _GetNeededSteps() {
            return 1;
        }
        #endregion

        #region Overrides
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
        #endregion
    }
}