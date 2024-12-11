using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Experimental.Tasks
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TaskScheduler), ExecutionOrder)]
    public class TaskScheduler : TlpSingleton
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.RecordingEnd - 2;
        #endregion

        public const string GameObjectName = "TLP_TaskScheduler";
        internal readonly DataList PendingTasks = new DataList();
        internal readonly DataDictionary UniqueTasks = new DataDictionary();
        private double _timeCompleted;
        private double _realDeltaTime;

        [FormerlySerializedAs("TimeBudget")]
        [Tooltip(
                "Minimum time to spend on tasks per frame (seconds), " +
                "if there is spare time there is automatically more time allocated, " +
                "but no more than fixedDeltaTime. " +
                "When set to 0, at least one task gets an update step.")]
        [Range(0, 0.05f)]
        public float MinTimeBudget = 0.0005f;

        #region State
        private int _taskIndex;
        #endregion

        public bool AddTask(TlpBaseBehaviour instigator, Task task) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(
                    $"{nameof(AddTask)}: instigator={instigator.GetScriptPathInScene()}, task={task.GetScriptPathInScene()}");
#endif
            #endregion

            if (!Utilities.IsValid(instigator)) {
                Error($"{nameof(instigator)} invalid");
                return false;
            }

            if (!Utilities.IsValid(task)) {
                Error($"{nameof(task)} invalid");
                return false;
            }

            if (Utilities.IsValid(task.ActiveScheduler)) {
                Error(
                        $"{task.GetScriptPathInScene()} already is scheduled in {task.ActiveScheduler.GetScriptPathInScene()}");
                return false;
            }

            if (UniqueTasks.ContainsKey(task)) {
                Error($"{task.GetScriptPathInScene()} is already present");
                return false;
            }

            UniqueTasks.Add(task, instigator);
            PendingTasks.Add(task);

            task.PrepareForRun();
            task.ActiveScheduler = this;
            task.TaskInstigator = instigator;
            if (!enabled) {
                enabled = true;
            }

            return true;
        }

        #region "Integral" part of PID
        internal float I = 0.02f;

        [SerializeField]
        private float Result;

        private const float Growth = 1.01f;
        #endregion

        private void OnEnable() {
            Result = 0f;
            _timeCompleted = Time.realtimeSinceStartupAsDouble;
        }

        public override void PostLateUpdate() {
            base.PostLateUpdate();
            float startTime = Time.realtimeSinceStartup;

            // reduced PID controller to just "I"
            float currentError = Time.fixedUnscaledDeltaTime * Growth - (float)_realDeltaTime;
            Result = Mathf.Clamp(Result + I * currentError, 0, Time.fixedUnscaledDeltaTime);

            int iteration = 0;
            while (PendingTasks.Count > 0
                   && (iteration < 1
                       || Time.realtimeSinceStartup - startTime < MinTimeBudget ||
                       (Time.realtimeSinceStartup - startTime <= Result &&
                        Time.realtimeSinceStartup - _timeCompleted < Time.fixedUnscaledDeltaTime))) {
                _taskIndex.MoveIndexRightLooping(PendingTasks.Count);
                ++iteration;
                var pendingTask = PendingTasks[_taskIndex];
                if (pendingTask.IsNull) {
                    RemoveTask(null, _taskIndex);
                    _taskIndex.MoveIndexLeftLooping(PendingTasks.Count);
                    continue;
                }

                var task = (Task)pendingTask.Reference;
                if (!Utilities.IsValid(task)) {
                    RemoveTask(null, _taskIndex);
                    _taskIndex.MoveIndexLeftLooping(PendingTasks.Count);
                    continue;
                }

                if (task.State == TaskState.Pending) {
                    if (!task.PrepareForRun()) {
                        RemoveTask(task, _taskIndex);
                        _taskIndex.MoveIndexLeftLooping(PendingTasks.Count);
                        continue;
                    }
                }

                if (task.Run() != TaskState.Finished) {
                    continue;
                }

                RemoveTask(task, _taskIndex);
                _taskIndex.MoveIndexLeftLooping(PendingTasks.Count);
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            if (PendingTasks.Count > 0) {
                Info($"{nameof(PostLateUpdate)}: Completed {iteration} task iterations");
            }
#endif
            #endregion

            _realDeltaTime = Time.realtimeSinceStartupAsDouble - _timeCompleted;
            _timeCompleted = Time.realtimeSinceStartupAsDouble;
        }

        private void RemoveTask(Task task, int index) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(RemoveTask)} index {index}");
#endif
            #endregion

            if (index < 0 || index >= PendingTasks.Count) {
                Error($"{nameof(RemoveTask)}: {nameof(index)} out of range");
                return;
            }

            PendingTasks.RemoveAt(index);
            if (!Utilities.IsValid(task)) {
                RemoveAllInvalidTasks();
            } else {
                bool hasInstigatorEntry = UniqueTasks.TryGetValue(task, TokenType.Reference, out var instigatorToken);

                // remove first before notifying instigator, in case the task will be added again
                if (!UniqueTasks.Remove(task)) {
                    Error(
                            $"{nameof(RemoveTask)}: Failed to remove {task.GetScriptPathInScene()} from {nameof(UniqueTasks)}");
                }

                bool wasActive = Utilities.IsValid(task.ActiveScheduler);
                task.ActiveScheduler = null;
                task.TaskInstigator = null;

                if (wasActive && hasInstigatorEntry) {
                    if (!instigatorToken.IsNull) {
                        var instigator = (TlpBaseBehaviour)instigatorToken.Reference;
                        if (Utilities.IsValid(instigator)) {
                            instigator.EventInstigator = this;
                            instigator.OnEvent("OnTaskFinished");
                            instigator.EventInstigator = null;
                        } else {
                            Error(
                                    $"{nameof(RemoveTask)}: {nameof(instigator)} invalid, {task.GetScriptPathInScene()} has no owner");
                        }
                    }
                }
            }

            if (PendingTasks.Count == 0 && enabled) {
                enabled = false;
            }
        }

        private void RemoveAllInvalidTasks() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RemoveAllInvalidTasks));
#endif
            #endregion

            var keys = UniqueTasks.GetKeys();
            for (int i = 0; i < keys.Count; i++) {
                var dataToken = keys[i];
                if (dataToken.IsNull || !Utilities.IsValid((Task)dataToken.Reference)) {
                    if (!UniqueTasks.Remove(dataToken)) {
                        Error(
                                $"{nameof(RemoveAllInvalidTasks)}: Failed to remove {dataToken} from {nameof(UniqueTasks)}");
                    }
                }
            }
        }

        public static bool AddTaskToDefaultScheduler(TlpBaseBehaviour instigator, Task task) {
            if (!Utilities.IsValid(task)) {
                TlpLogger.StaticError($"{nameof(task)} invalid", null);
                return false;
            }

            if (!Utilities.IsValid(task.DefaultScheduler)) {
                task.DefaultScheduler = GetInstance<TaskScheduler>(GameObjectName);
                if (!Utilities.IsValid(task.DefaultScheduler)) {
                    TlpLogger.StaticError($"{GameObjectName} not found in the scene", null);
                    return false;
                }
            }

            return task.DefaultScheduler.AddTask(instigator, task);
        }

        public void CancelTask(Task task) {
            if (Utilities.IsValid(task)) {
                RemoveTask(task, PendingTasks.IndexOf(task));
            }
        }
    }
}