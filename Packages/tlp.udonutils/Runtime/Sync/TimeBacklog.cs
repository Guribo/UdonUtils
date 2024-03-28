using UdonSharp;
using VRC.SDK3.Data;

namespace TLP.UdonUtils.Sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class TimeBacklog : TlpBaseBehaviour
    {
        protected readonly DataList _timeStamps = new DataList();

        public virtual void Add(TimeSnapshot snapshot, float maxAge) {
            DebugLog($"{nameof(Add)}: {nameof(maxAge)} = {maxAge}s");
            _timeStamps.Add(snapshot.ServerTime);
            while (_timeStamps.Count > 0 && snapshot.ServerTime - _timeStamps[0].Float > maxAge) {
                _timeStamps.RemoveAt(0);
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"Backlog of timestamps: ${_timeStamps.Count}");
#endif
            #endregion
        }


        public bool Interpolatable(float serverTime) {
            DebugLog($"{nameof(Interpolatable)}: {nameof(serverTime)} = {serverTime}s");
            return _timeStamps.Count > 1
                   && serverTime > _timeStamps[0].Float
                   && serverTime < _timeStamps[_timeStamps.Count - 1].Float;
        }
    }
}