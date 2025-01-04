﻿using JetBrains.Annotations;
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
    public class TaskScheduler : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.RecordingEnd - 2;
        #endregion

        #region Constants
        public const string FinishedTaskVariableName = "FinishedTask";
        public const string FinishedTaskCallbackName = "OnTaskFinished";
        public const string GameObjectName = "TLP_TaskScheduler";

        private const float Centimeter = 0.01f;
        private const int MinHeadIdleFrames = 50;
        #endregion

        #region State
        internal readonly DataList PendingTasks = new DataList();
        internal readonly DataDictionary UniqueTasks = new DataDictionary();
        private double _timeCompleted;
        private double _realDeltaTime;
        private int _taskIndex;
        private Task _taskWithMostSteps;
        private Vector3 _lastHeadPosition;
        private Quaternion _lastHeadRotation;
        private int _lastNotIdle;
        private bool _wasIdle;

        #region "Integral" part of PID
        internal float I = 0.02f;

        [SerializeField]
        private float Result;

        private const float Growth = 1.01f;
        #endregion
        #endregion

        #region Configuration
        [FormerlySerializedAs("TimeBudget")]
        [Tooltip(
                "Minimum time to spend on tasks per frame (seconds), " +
                "if there is spare time there is automatically more time allocated, " +
                "but no more than fixedDeltaTime. " +
                "When set to 0, at least one task gets an update step.")]
        [Range(0, 0.05f)]
        public float MinTimeBudget = 0.0005f;

        [Tooltip(
                "Controls how aggressive tasks are to be processed. " +
                "When set to e.g. 2 it will try to maintain 45fps if the base framerate is 90fps, " +
                "when set to 4 it will try to reach 22.5fps. Setting this value >1 will speed up " +
                "task-completion at the cost of fps if the player is usually above the target framerate. " +
                "Has no effect if the player is already below the target framerate. " +
                "Use MinTimeBudget to speed up the processing here instead.")]
        [Range(1f, 8f)]
        public float DynamicLimit = 1f;

        [Tooltip(
                "Same as DynamicLimit except it is only used when DynamicIdleLimit > DynamicLimit and " +
                "player head is detected to be not moving much.")]
        [Range(1f, 8f)]
        public float DynamicIdleLimit = 4f;
        #endregion

        #region Lifecycle
        private void OnEnable() {
            Result = 0f;
            _timeCompleted = Time.realtimeSinceStartupAsDouble;
        }

        public override void PostLateUpdate() {
            base.PostLateUpdate();

            if (PendingTasks.Count < 1) {
                enabled = false;
                return;
            }

            float startTime = Time.realtimeSinceStartup;
            float dynamicLimit = DynamicIdleLimit > DynamicLimit && IsPlayerIdle() ? DynamicIdleLimit : DynamicLimit;

            // reduced PID controller to just "I"
            float targetDeltaTime = Time.fixedUnscaledDeltaTime * dynamicLimit;
            float currentError = targetDeltaTime * Growth - (float)_realDeltaTime;
            Result = Mathf.Clamp(Result + I * currentError, 0, targetDeltaTime);

            int iteration = 0;
            while (PendingTasks.Count > 0
                   && (iteration < 1
                       || Time.realtimeSinceStartup - startTime < MinTimeBudget ||
                       (Time.realtimeSinceStartup - startTime <= Result &&
                        Time.realtimeSinceStartup - _timeCompleted < targetDeltaTime))) {
                float iterationStartTime = Time.realtimeSinceStartup;

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

                float remainingTime = Result - (Time.realtimeSinceStartup - startTime);
                if (iteration > 1 && remainingTime < task.EstimatedStepDuration) {
                    if (_taskIndex == 0) {
                        // prevent infinite looping in case every single task would exceed the time budget
                        break;
                    }
                    // skip heavy task in 2nd or later iterations as it would go over time budget
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
                    if (!ReferenceEquals(task, _taskWithMostSteps)
                        && (!Utilities.IsValid(_taskWithMostSteps)
                            || _taskWithMostSteps.State == TaskState.Finished
                            || _taskWithMostSteps.GetNeededSteps() < task.GetNeededSteps())) {
                        _taskWithMostSteps = task;
                    }
                } else {
                    RemoveTask(task, _taskIndex);
                    _taskIndex.MoveIndexLeftLooping(PendingTasks.Count);
                }

                if (Utilities.IsValid(task)) {
                    // 1. check in case the task was destroyed in RemoveTask after notifying the instigator
                    // 2. update average time to prevent overshoot
                    task.UpdateEstimatedStepDuration(Time.realtimeSinceStartup - iterationStartTime);
                }
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
        #endregion

        #region Internal
        private bool IsPlayerIdle() {
            var player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player)) return false;
            var head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            bool headsIsNotMoving = (head.position - _lastHeadPosition).magnitude < Centimeter * Time.deltaTime;
            bool headIsNotRotating = Quaternion.Angle(head.rotation, _lastHeadRotation) < /* 1 degree/second*/
                                     Time.deltaTime;
            bool isIdle = headsIsNotMoving && headIsNotRotating;
            _lastHeadPosition = head.position;
            _lastHeadRotation = head.rotation;
            if (!isIdle) {
                if (_wasIdle) {
                    // as soon as player movement is detected again, after bing idle,
                    // reset PID result to prevent any hitching due to slow adjustment of PID loop
                    Result = 0;
                    _wasIdle = false;
                }

                _lastNotIdle = Time.frameCount;
                return false;
            }

            if (Time.frameCount < _lastNotIdle + MinHeadIdleFrames) {
                return false;
            }

            _wasIdle = true;
            return true;
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

                TlpBaseBehaviour instigator = null;
                if (wasActive && hasInstigatorEntry) {
                    if (!instigatorToken.IsNull) {
                        instigator = (TlpBaseBehaviour)instigatorToken.Reference;
                        if (Utilities.IsValid(instigator)) {
                            instigator.EventInstigator = this;
                            instigator.SetProgramVariable(FinishedTaskVariableName, task);
                            instigator.OnEvent(FinishedTaskCallbackName);
                            instigator.EventInstigator = null;
                            instigator.SetProgramVariable(FinishedTaskVariableName, null);
                        } else {
                            Error(
                                    $"{nameof(RemoveTask)}: {nameof(instigator)} invalid, {task.GetScriptPathInScene()} has no owner");
                        }
                    }
                }

                if (task.State != TaskState.Finished) {
                    #region TLP_DEBUG
#if TLP_DEBUG
                    Warn(
                            $"{nameof(RemoveTask)}: {nameof(task)}={task.GetScriptPathInScene()} seems to have been re-scheduled by '{instigator.GetScriptPathInScene()}' before cleanup could occur");
#endif
                    #endregion
                } else if (!task.DeInitTask()) {
                    Error($"{nameof(RemoveTask)}: Failed to de-init {task.GetScriptPathInScene()} after completion");
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
        #endregion

        #region Public API
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

            task.ActiveScheduler = this;
            task.TaskInstigator = instigator;

            if (!task.PrepareForRun()) {
                Error($"{nameof(AddTask)}: failed to prepare for run");

                task.ActiveScheduler = null;
                task.TaskInstigator = null;
                return false;
            }

            UniqueTasks.Add(task, instigator);
            PendingTasks.Add(task);
            if (!enabled) {
                enabled = true;
            }

            return true;
        }

        public static bool AddTaskToDefaultScheduler(TlpBaseBehaviour instigator, Task task) {
            if (!Utilities.IsValid(task)) {
                TlpLogger.StaticError($"{nameof(task)} invalid", null);
                return false;
            }

            if (!Utilities.IsValid(task.DefaultScheduler)) {
                task.DefaultScheduler = TlpSingleton.GetInstance<TaskScheduler>(GameObjectName);
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

        public float GetProgress(out Task longestTask) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(GetProgress)}: {nameof(_taskWithMostSteps)}={_taskWithMostSteps.GetScriptPathInScene()}");
#endif
            #endregion

            if (Utilities.IsValid(_taskWithMostSteps)) {
                longestTask = _taskWithMostSteps;
                return longestTask.Progress;
            }

            longestTask = null;
            return 0f;
        }
        #endregion
    }
}