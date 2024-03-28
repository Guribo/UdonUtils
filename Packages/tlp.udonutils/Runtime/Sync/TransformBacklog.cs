using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonUtils.Sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TransformBacklog : TimeBacklog
    {
        private readonly DataList _positionBackLog = new DataList();
        private readonly DataList _rotationBackLog = new DataList();

        public bool Interpolate(float time, out Vector3 position, out Quaternion rotation) {
            DebugLog($"{nameof(Interpolate)}: {nameof(time)} = {time}s");
            int index = 0;
            while (index < _timeStamps.Count - 1 && _timeStamps[index].Float < time) {
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

            if (index - 1 < 0) {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

            if (index < 0 || index >= _timeStamps.Count) {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

            float timeStampA = _timeStamps[index].Float;
            float timeStampB = _timeStamps[index - 1].Float;

            var positionToken3 = _positionBackLog[index - 1];
            var rotationToken3 = _rotationBackLog[index - 1];
            if (positionToken3.Error != DataError.None || rotationToken3.Error != DataError.None) {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

            if (Mathf.Abs(timeStampA - timeStampB) < 0.001f) {
                position = (Vector3)positionToken3.Reference;
                rotation = (Quaternion)rotationToken3.Reference;
                return true;
            }

            float interpolationRatio = Mathf.InverseLerp(
                    timeStampA - Time.timeSinceLevelLoad,
                    timeStampB - Time.timeSinceLevelLoad,
                    time - Time.timeSinceLevelLoad
            );

            var positionToken2 = _positionBackLog[index];
            var rotationToken2 = _rotationBackLog[index];
            if (positionToken2.Error != DataError.None || rotationToken2.Error != DataError.None) {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

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

        public override void Add(TimeSnapshot snapshot, float maxAge) {
            base.Add(snapshot, maxAge);
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
            DebugLog($"Backlog of transformData: ${_positionBackLog.Count}; {_rotationBackLog.Count}");
#endif
            #endregion
        }
    }
}