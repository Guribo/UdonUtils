using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sources;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestMaxSendRateSender), ExecutionOrder)]
    public class TestMaxSendRateSender : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestMaxSendRate.ExecutionOrder + 1;

        public TimeSource TimeSource;

        [UdonSynced]
        public double SendTime;

        public TestMaxSendRate Test;

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(TimeSource)) {
                _Error($"{nameof(TimeSource)} is not set");
                return false;
            }

            AutoRetrySendOnFailure = false;


            return true;
        }

        public override void OnPreSerialization() {
            base.OnPreSerialization();
            SendTime = TimeSource._TimeAsDouble();
        }

        public override void OnDeserialization(DeserializationResult result) {
            base.OnDeserialization(result);
            Test._ReceivedData(this);
        }
    }
}