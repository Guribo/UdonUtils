﻿using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace TLP.UdonUtils.Sync
{
    /// <summary>
    /// Allows adding position and rotation snapshots for interpolation.
    /// Note that the time values must be monotonic rising, otherwise adding will fail.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TransformBacklog : TimeBacklog
    {
        private readonly DataList _positionBackLog = new DataList();
        private readonly DataList _rotationBackLog = new DataList();

        public bool Interpolate(float time, out Vector3 position, out Quaternion rotation) {
            DebugLog($"{nameof(Interpolate)}: {nameof(time)} = {time}s");
            int index = 0;
            int lastIndex = _timeStamps.Count - 1;
            while (index < lastIndex && _timeStamps[index].Float < time) {
                ++index;
            }

            if (index == 0) {
                var positionToken = _positionBackLog[0];
                var rotationToken = _rotationBackLog[0];
                if (positionToken.Error == DataError.None
                    && rotationToken.Error == DataError.None) {
                    position = (Vector3)positionToken.Reference;
                    rotation = (Quaternion)rotationToken.Reference;
                    return true;
                }

                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

            float timeStampA = _timeStamps[index].Float;
            int previousIndex = index - 1;
            float timeStampB = _timeStamps[previousIndex].Float;

            var positionToken3 = _positionBackLog[previousIndex];
            var rotationToken3 = _rotationBackLog[previousIndex];

            float interpolationRatio = Mathf.InverseLerp(
                    timeStampA - Time.timeSinceLevelLoad,
                    timeStampB - Time.timeSinceLevelLoad,
                    time - Time.timeSinceLevelLoad
            );

            var positionToken2 = _positionBackLog[index];
            var rotationToken2 = _rotationBackLog[index];

            position = Vector3.Lerp(
                    (Vector3)positionToken2.Reference,
                    (Vector3)positionToken3.Reference,
                    interpolationRatio);
            rotation = Quaternion.Slerp(
                    (Quaternion)rotationToken2.Reference,
                    (Quaternion)rotationToken3.Reference,
                    interpolationRatio);

            return true;
        }

        public override bool Add(TimeSnapshot snapshot, float maxAge) {
            if (!base.Add(snapshot, maxAge)) {
                return false;
            }

            var transformSnapShot = (TransformSnapshot)snapshot;

            _positionBackLog.Add(new DataToken(transformSnapShot.Position));
            _rotationBackLog.Add(new DataToken(transformSnapShot.Rotation));
            while (_positionBackLog.Count > _timeStamps.Count) {
                _positionBackLog.RemoveAt(0);
            }

            while (_rotationBackLog.Count > _timeStamps.Count) {
                _rotationBackLog.RemoveAt(0);
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"Backlog of transformData: {_positionBackLog.Count}; {_rotationBackLog.Count}");
#endif
            #endregion

            return true;
        }
    }
}