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
    public class TaskScheduler : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.RecordingEnd - 2;
        #endregion

        #region Constants
        public const string FinishedTaskVariableName = "FinishedTask";
        public const string FinishedTaskCallbackName = Task.FinishedTaskCallbackName;
        public const string GameObjectName = "TLP_TaskScheduler";

        private const int MinPlayerIdleFrames = 50;
        #endregion

        #region State
        internal readonly DataList PendingTasks = new DataList();
        internal readonly DataDictionary UniqueTasks = new DataDictionary();
        private double _timeCompleted;
        private double _realDeltaTime;
        private int _taskIndex;
        private Task _taskWithMostSteps;
        private Vector3 _lastHeadPosition, _lastLeftHandPosition, _lastRightHandPosition;
        private Quaternion _lastHeadRotation, _lastLeftHandRotation, _lastRightHandRotation;
        private int _lastNotIdle;
        private bool _wasIdle;
        private float _averageIterations;

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

        [Tooltip(
                "[m/s]; In desktop movement speed of the player head below which the player is considered idle. "
                + "In VR additionally used for checking hand movement.")]
        public float PlayerIdleMovementSpeed = 0.03f;

        [Tooltip(
                "[degrees/s]; In desktop turn speed of the camera below which the player is considered idle. "
                + "In VR used for checking hand and head rotation speed.")]
        public float PlayerIdleTurnSpeed = 3f;

        public int MinIterations = 1;
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

            double startTime = Time.realtimeSinceStartupAsDouble;
            float dynamicLimit = DynamicIdleLimit > DynamicLimit && _IsPlayerIdle() ? DynamicIdleLimit : DynamicLimit;

            // reduced PID controller to just "I"
            float targetDeltaTime = Time.fixedDeltaTime * dynamicLimit;
            float currentError = targetDeltaTime * Growth - (float)_realDeltaTime;
            Result = Mathf.Clamp(Result + I * currentError, 0, targetDeltaTime);

            int iteration = 0;
            int skipCount = 0;
            int totalSkips = 0;
            // ensure we start with a different task each frame
            _taskIndex = Time.frameCount % Mathf.Max(1, PendingTasks.Count);
            while (true) {
                if (PendingTasks.Count < 1) break;

                _taskIndex._MoveIndexRightLooping(PendingTasks.Count);
                var pendingTask = PendingTasks[_taskIndex];
                if (pendingTask.IsNull) {
                    _RemoveTask(null, _taskIndex);
                    _taskIndex._MoveIndexLeftLooping(PendingTasks.Count);
                    continue;
                }

                var task = (Task)pendingTask.Reference;
                if (!Utilities.IsValid(task)) {
                    _RemoveTask(null, _taskIndex);
                    _taskIndex._MoveIndexLeftLooping(PendingTasks.Count);
                    continue;
                }

                double iterationStartTime = Time.realtimeSinceStartupAsDouble;
                ++iteration;
                if (iteration > MinIterations) {
                    double spentTime = iterationStartTime - startTime;
                    double timeSinceLastCompletion = iterationStartTime - _timeCompleted;
                    if (spentTime > MinTimeBudget &&
                        (spentTime > Result || timeSinceLastCompletion > targetDeltaTime)) break;

                    double remainingTime = Mathf.Max(MinTimeBudget, Result) - spentTime;
                    if (remainingTime < task.EstimatedStepDuration) {
                        #region TLP_DEBUG
#if TLP_DEBUG
                        _DebugLog($"{nameof(PostLateUpdate)}: {nameof(task)}={task._GetScriptPathInScene()} would take too long");
#endif
                        #endregion
                        ++skipCount;
                        ++totalSkips;
                        if (skipCount >= PendingTasks.Count) {
                            // prevent infinite looping in case every single task would exceed the time budget
                            break;
                        }

                        // skip heavy task in 2nd or later iterations as it would go over time budget
                        continue;
                    }
                }

                if (task.State == TaskState.Pending) {
                    if (!task._PrepareForRun()) {
                        _RemoveTask(task, _taskIndex);
                        _taskIndex._MoveIndexLeftLooping(PendingTasks.Count);
                        continue;
                    }
                }

                var taskState = task._Run();
                if (task.Result != TaskResult.Blocked) {
                    skipCount = 0;
                } else {
                    #region TLP_DEBUG
#if TLP_DEBUG
                    _DebugLog($"{nameof(PostLateUpdate)}: {nameof(task)}={task._GetScriptPathInScene()} is blocked");
#endif
#endregion
                    ++skipCount;
                    ++totalSkips;
                    if (skipCount >= PendingTasks.Count) {
                        // prevent infinite looping in case every single task would exceed the time budget
                        break;
                    }

                    // skip heavy task in 2nd or later iterations as it would go over time budget
                    continue;
                }

                if (taskState != TaskState.Finished) {
                    if (!ReferenceEquals(task, _taskWithMostSteps)
                        && (!Utilities.IsValid(_taskWithMostSteps)
                            || _taskWithMostSteps.State == TaskState.Finished
                            || _taskWithMostSteps._GetNeededSteps() < task._GetNeededSteps())) {
                        _taskWithMostSteps = task;
                    }
                } else {
                    _RemoveTask(task, _taskIndex);
                    _taskIndex._MoveIndexLeftLooping(PendingTasks.Count);
                }

                if (Utilities.IsValid(task)) {
                    // 1. check in case the task was destroyed in RemoveTask after notifying the instigator
                    // 2. update average time to prevent overshoot in the future
                    task._UpdateEstimatedStepDuration((float)(Time.realtimeSinceStartupAsDouble - iterationStartTime));
                }
            }

            double newTimeCompleted = Time.realtimeSinceStartupAsDouble;
            _realDeltaTime = newTimeCompleted - _timeCompleted;
            _timeCompleted = newTimeCompleted;
            _averageIterations = Mathf.Lerp(_averageIterations, iteration, 0.1f);
#if TLP_DEBUG
            if (Time.frameCount % 100 == 0) {
                _Info(
                        $"{nameof(PostLateUpdate)}: Completed {iteration} iterations in {1000 * (newTimeCompleted - startTime):F6}ms, skipped task steps={totalSkips}, avg. iterations={_averageIterations:F1}, real dt={1000 * _realDeltaTime:F3}ms");
            }
#endif
        }
        #endregion

        #region Internal
        private bool _IsPlayerIdle() {
            var player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player)) return false;
            var head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            float playerIdleMovementSpeed = PlayerIdleMovementSpeed * Time.deltaTime;
            float playerIdleTurnSpeed = PlayerIdleTurnSpeed * Time.deltaTime;

            bool headsIsNotMoving = (head.position - _lastHeadPosition).magnitude < playerIdleMovementSpeed;
            bool headIsNotRotating = Quaternion.Angle(head.rotation, _lastHeadRotation) < playerIdleTurnSpeed;
            _lastHeadPosition = head.position;
            _lastHeadRotation = head.rotation;
            bool isIdle = headsIsNotMoving && headIsNotRotating;

            if (isIdle && player.IsUserInVR()) {
                var leftHand = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
                var rightHand = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);

                bool leftHandIsNotMoving =
                        (leftHand.position - _lastLeftHandPosition).magnitude < playerIdleMovementSpeed;
                bool leftHandIsNotRotating =
                        Quaternion.Angle(leftHand.rotation, _lastLeftHandRotation) < playerIdleTurnSpeed;

                bool rightHandIsNotMoving =
                        (rightHand.position - _lastRightHandPosition).magnitude < playerIdleMovementSpeed;
                bool rightHandIsNotRotating =
                        Quaternion.Angle(rightHand.rotation, _lastRightHandRotation) < playerIdleTurnSpeed;

                isIdle = leftHandIsNotMoving && leftHandIsNotRotating && rightHandIsNotMoving && rightHandIsNotRotating;

                _lastLeftHandPosition = leftHand.position;
                _lastRightHandPosition = rightHand.position;
                _lastLeftHandRotation = leftHand.rotation;
                _lastRightHandRotation = rightHand.rotation;
            }

            if (!isIdle) {
                if (_wasIdle) {
                    // as soon as player movement is detected again, after being idle,
                    // reset PID result to prevent any hitching due to slow adjustment of PID loop
                    Result = 0;
                    _wasIdle = false;
                }

                _lastNotIdle = Time.frameCount;
                return false;
            }

            if (Time.frameCount < _lastNotIdle + MinPlayerIdleFrames) {
                return false;
            }

            _wasIdle = true;
            return true;
        }

        private void _RemoveTask(Task task, int index) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_RemoveTask)} index {index}");
#endif
            #endregion

            if (index < 0 || index >= PendingTasks.Count) {
                _Error($"{nameof(_RemoveTask)}: {nameof(index)} out of range");
                return;
            }

            PendingTasks.RemoveAt(index);
            if (!Utilities.IsValid(task)) {
                _RemoveAllInvalidTasks();
            } else {
                bool hasInstigatorEntry = UniqueTasks.TryGetValue(task, TokenType.Reference, out var instigatorToken);

                // remove first before notifying instigator, in case the task will be added again
                if (!UniqueTasks.Remove(task)) {
                    _Error(
                            $"{nameof(_RemoveTask)}: Failed to remove {task._GetScriptPathInScene()} from {nameof(UniqueTasks)}");
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
                            instigator._OnEvent(FinishedTaskCallbackName);
                            instigator.EventInstigator = null;
                            instigator.SetProgramVariable(FinishedTaskVariableName, null);
                        } else {
                            _Error(
                                    $"{nameof(_RemoveTask)}: {nameof(instigator)} invalid, {task._GetScriptPathInScene()} has no owner");
                        }
                    }
                }

                if (task.State != TaskState.Finished) {
                    #region TLP_DEBUG
#if TLP_DEBUG
                    _Warn(
                            $"{nameof(_RemoveTask)}: {nameof(task)}={task._GetScriptPathInScene()} seems to have been re-scheduled by '{instigator._GetScriptPathInScene()}' before cleanup could occur");
#endif
                    #endregion
                } else if (!task._DeInitTask()) {
                    _Error($"{nameof(_RemoveTask)}: Failed to de-init {task._GetScriptPathInScene()} after completion");
                }
            }

            if (PendingTasks.Count == 0 && enabled) {
                enabled = false;
            }
        }

        private void _RemoveAllInvalidTasks() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_RemoveAllInvalidTasks));
#endif
            #endregion

            var keys = UniqueTasks.GetKeys();
            for (int i = 0; i < keys.Count; i++) {
                var dataToken = keys[i];
                if (dataToken.IsNull || !Utilities.IsValid((Task)dataToken.Reference)) {
                    if (!UniqueTasks.Remove(dataToken)) {
                        _Error(
                                $"{nameof(_RemoveAllInvalidTasks)}: Failed to remove {dataToken} from {nameof(UniqueTasks)}");
                    }
                }
            }
        }
        #endregion

        #region Public API
        public bool _AddTask(TlpBaseBehaviour instigator, Task task) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(
                    $"{nameof(_AddTask)}: instigator={instigator._GetScriptPathInScene()}, task={task._GetScriptPathInScene()}");
#endif
            #endregion

            if (!Utilities.IsValid(instigator)) {
                _Error($"{nameof(instigator)} invalid");
                return false;
            }

            if (!Utilities.IsValid(task)) {
                _Error($"{nameof(task)} invalid");
                return false;
            }

            if (Utilities.IsValid(task.ActiveScheduler)) {
                _Error(
                        $"{task._GetScriptPathInScene()} already is scheduled in {task.ActiveScheduler._GetScriptPathInScene()}");
                return false;
            }

            if (UniqueTasks.ContainsKey(task)) {
                _Error($"{task._GetScriptPathInScene()} is already present");
                return false;
            }

            task.ActiveScheduler = this;
            task.TaskInstigator = instigator;

            if (!task._PrepareForRun()) {
                _Error($"{nameof(_AddTask)}: failed to prepare for run");

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

        public static bool _AddTaskToDefaultScheduler(TlpBaseBehaviour instigator, Task task) {
            if (!Utilities.IsValid(task)) {
                TlpLogger.StaticError($"{nameof(task)} invalid", null);
                return false;
            }

            if (!Utilities.IsValid(task.DefaultScheduler)) {
                task.DefaultScheduler = TlpSingleton._GetInstance<TaskScheduler>(GameObjectName);
                if (!Utilities.IsValid(task.DefaultScheduler)) {
                    TlpLogger.StaticError($"{GameObjectName} not found in the scene", null);
                    return false;
                }
            }

            return task.DefaultScheduler._AddTask(instigator, task);
        }

        public void _CancelTask(Task task) {
            if (Utilities.IsValid(task)) {
                _RemoveTask(task, PendingTasks.IndexOf(task));
            }
        }

        public float _GetProgress(out Task longestTask) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_GetProgress)}: {nameof(_taskWithMostSteps)}={_taskWithMostSteps._GetScriptPathInScene()}");
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