using System;
using System.Diagnostics;
using JetBrains.Annotations;
using TLP.UdonUtils.Extensions;
using TLP.UdonUtils.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class NetworkTime : TlpBaseBehaviour
    {
        [SerializeField]
        private bool UseOwnServerTime;

        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpLogger.ExecutionOrder + 1;

        [UdonSynced]
        [SerializeField]
        private double SyncedMasterTime;

        private int _lastUpdate;
        private double _vrcServerTime;
        private readonly Stopwatch _realTime = new Stopwatch();
        private double _latestRealTimeSnapshot;
        private double _serverTimeCurrentFrame;
        private double _vrcServerTimeCurrentFrame;
        private float _nextSerialization;
        private float _realTimeCurrentFrame;


        [SerializeField]
        private float SyncInterval = 15f;

        private void OnEnable()
        {
            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif

            #endregion

            _realTime.Start();
            _vrcServerTime = Networking.GetServerTimeInMilliseconds() * 0.001;
            _CreateNewSnapshot(0, 0);
        }

        public double GetTimeForCurrentFrame()
        {
            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog(nameof(GetTimeForCurrentFrame));
#endif

            #endregion

            double timeForCurrentFrame = _GetTimeForCurrentFrame();
            if (Math.Abs(_averageDelta) > 0.01)
            {
                // temporarily return the less accurate time with ms accuracy until the _averageDelta has settled
                Warn($"Time accuracy still exceeds 10 ms: {_averageDelta * 1000}ms");
                return Networking.GetServerTimeInMilliseconds() * 0.001;
            }

            return timeForCurrentFrame + _averageDelta;
        }

        public float GetRealTimeForCurrentFrame()
        {
            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog(nameof(GetRealTimeForCurrentFrame));
#endif

            #endregion


            double _ = _GetTimeForCurrentFrame();

            return _realTimeCurrentFrame;
        }

        public double GetVrcServerTimeForCurrentFrame()
        {
            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog(nameof(GetVrcServerTimeForCurrentFrame));
#endif

            #endregion


            double _ = _GetTimeForCurrentFrame();

            return _vrcServerTimeCurrentFrame;
        }

        public double GetVrcServerTimeDelta => _vrcServerTimeCurrentFrame - GetTimeForCurrentFrame();

        private double _GetTimeForCurrentFrame()
        {
            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog(nameof(_GetTimeForCurrentFrame));
#endif

            #endregion

            if (Time.frameCount == _lastUpdate)
            {
                // already calculated
                return _serverTimeCurrentFrame;
            }

            _lastUpdate = Time.frameCount;
            _realTimeCurrentFrame = Time.realtimeSinceStartup;
            _vrcServerTimeCurrentFrame = Networking.GetServerTimeInMilliseconds() * 0.001;
            double secondsSinceStart = _realTime.Elapsed.TotalSeconds;
            double secondsSinceLastSnapShot = secondsSinceStart - _latestRealTimeSnapshot;
            if (Math.Abs(secondsSinceLastSnapShot) > SyncInterval)
            {
                _CreateNewSnapshot(secondsSinceStart, secondsSinceLastSnapShot);
            }
            else
            {
                _serverTimeCurrentFrame = _vrcServerTime + secondsSinceLastSnapShot;
            }

            if (UseOwnServerTime && Networking.IsOwner(gameObject) && Time.timeSinceLevelLoad > _nextSerialization)
            {
                _nextSerialization = Time.timeSinceLevelLoad + 0.5f;
                RequestSerialization();
            }

            return _serverTimeCurrentFrame;
        }

        private void _CreateNewSnapshot(double secondsSinceStart, double secondsSinceLastSnapShot)
        {
            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog(nameof(_CreateNewSnapshot));
#endif

            #endregion

            _latestRealTimeSnapshot = secondsSinceStart;

            double expectedTime = _vrcServerTime + secondsSinceLastSnapShot;
            _vrcServerTime = _vrcServerTime + secondsSinceLastSnapShot + _averageDelta;
            _averageDelta = 0;

            double drift = expectedTime - _vrcServerTime;
            if (Math.Abs(drift) > 0.01)
            {
                Warn($"Time drift exceeded 10 ms: {drift * 1000} ms");
            }
#if TLP_DEBUG
            DebugLog($"Time drift: {drift * 1000} ms");
#endif
            _serverTimeCurrentFrame = _vrcServerTime;
        }

        private double _averageDelta;
        private void Update()
        {
            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog(nameof(Update));
#endif

            #endregion

            if (UseOwnServerTime)
            {
                if (Networking.LocalPlayer.isMaster && !Networking.IsOwner(gameObject))
                {
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                }

                if (!Networking.IsOwner(gameObject))
                {
                    return;
                }
            }


            _UpdateDrift(GetVrcServerTimeForCurrentFrame());
        }

        private void _UpdateDrift(double vrcServerTime)
        {
            double currentTime = _GetTimeForCurrentFrame();
            double delta = vrcServerTime - currentTime;
            if (Math.Abs(_averageDelta - delta) > 1)
            {
                _averageDelta = delta;
            }
            else
            {
                _averageDelta = 0.001 * delta + 0.999 * _averageDelta;
            }
#if TLP_DEBUG
            if (Time.frameCount % 60 == 0)
            {
                Info(
                    $"Real nt: {vrcServerTime:F5}s; " +
                    $"Own nt: {currentTime:F5}s; " +
                    $"Corrected own nt: {GetTimeForCurrentFrame():F5}s; " +
                    $"dt: {delta * 1000:F2}ms; " +
                    $"Avg. dt: {_averageDelta * 1000:F2}ms"
                );
            }
#endif
        }


        public override void OnPreSerialization()
        {
            base.OnPreSerialization();
            if (UseOwnServerTime)
            {
                SyncedMasterTime = _serverTimeCurrentFrame + (Time.realtimeSinceStartup - _realTimeCurrentFrame);
            }
        }

        public override void OnDeserialization(DeserializationResult deserializationResult)
        {
            base.OnDeserialization(deserializationResult);

            if (UseOwnServerTime)
            {
                double masterServerTime = deserializationResult.Latency() + SyncedMasterTime;
                _UpdateDrift(masterServerTime);
            }
        }
    }
}