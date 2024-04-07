using System;
using UdonSharp;
using VRC.SDK3.Data;

namespace TLP.UdonUtils.Sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class TimeBacklog : TlpBaseBehaviour
    {
        protected readonly DataList _timeStamps = new DataList();
        private float _lastAdded = float.MinValue;

        public virtual bool Add(TimeSnapshot snapshot, float maxAge) {
            DebugLog($"{nameof(Add)}: {nameof(maxAge)} = {maxAge}s");
            if (_lastAdded >= snapshot.ServerTime) {
                Warn(
                        $"Received snapshot with older timestamp, discarding: {_lastAdded}s > {snapshot.ServerTime}s");
                return false;
            }

            _timeStamps.Add(snapshot.ServerTime);
            _lastAdded = snapshot.ServerTime;
            while (_timeStamps.Count > 0 && (snapshot.ServerTime - _timeStamps[0].Float) > maxAge) {
                _timeStamps.RemoveAt(0);
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"Backlog of timestamps: {_timeStamps.Count}");
#endif
            #endregion

            return true;
        }


        public bool Interpolatable(float serverTime) {
            DebugLog($"{nameof(Interpolatable)}: {nameof(serverTime)} = {serverTime}s");
            if (_timeStamps.Count == 1 && Math.Abs(_timeStamps[0].Float - serverTime) < 1e-6) return true;

            return _timeStamps.Count > 1
                   && serverTime >= _timeStamps[0].Float
                   && serverTime <= _timeStamps[_timeStamps.Count - 1].Float;
        }
    }
}