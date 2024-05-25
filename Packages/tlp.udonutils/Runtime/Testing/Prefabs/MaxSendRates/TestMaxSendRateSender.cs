using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Sources;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TestMaxSendRateSender : TlpBaseBehaviour
{
    public TimeSource TimeSource;

    [UdonSynced]
    public double SendTime;

    public TestMaxSendRate Test;

    public override void Start() {
        base.Start();
        AutoRetrySendOnFailure = false;
    }

    protected override bool SetupAndValidate() {
        if (!base.SetupAndValidate()) {
            return false;
        }

        if (!Utilities.IsValid(TimeSource)) {
            Error($"{nameof(TimeSource)} is not set");
            return false;
        }

        return true;
    }

    public override void OnPreSerialization() {
        base.OnPreSerialization();
        SendTime = TimeSource.TimeAsDouble();
    }

    public override void OnDeserialization(DeserializationResult result) {
        base.OnDeserialization(result);
        Test.ReceivedData(this);
    }
}