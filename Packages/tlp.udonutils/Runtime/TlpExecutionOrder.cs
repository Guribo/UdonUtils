using VRC.SDK3.Data;

namespace TLP.UdonUtils.Runtime
{
    public static class TlpExecutionOrder
    {
        public const int Min = int.MinValue + 1_000_000;
        public const int Max = TestingEnd + 1;

        public const int DefaultOffset = 150_000;
        public const int WorldInitStart = -3_000 + DefaultOffset;
        public const int WorldInitEnd = -2_001 + DefaultOffset;
        public const int TimeSourcesStart = -2_000 + DefaultOffset;
        public const int TimeSourcesEnd = -1_001 + DefaultOffset;
        public const int DirectInputStart = -1_000 + DefaultOffset;
        public const int DirectInputEnd = -1 + DefaultOffset;
        public const int DefaultStart = 0 + DefaultOffset;
        public const int DefaultEnd = 999 + DefaultOffset;
        public const int VehicleMotionStart = 1_000 + DefaultOffset;
        public const int VehicleMotionEnd = 1_999 + DefaultOffset;
        public const int PlayerMotionStart = 2_000 + DefaultOffset;
        public const int PlayerMotionEnd = 2_999 + DefaultOffset;
        public const int WeaponsStart = 3_000 + DefaultOffset;
        public const int WeaponsEnd = 3_999 + DefaultOffset;
        public const int UiStart = 10_000 + DefaultOffset;
        public const int UiEnd = 10_999 + DefaultOffset;
        public const int AudioStart = 11_000 + DefaultOffset;
        public const int AudioEnd = 11_999 + DefaultOffset;
        public const int RecordingStart = 12_000 + DefaultOffset;
        public const int RecordingEnd = 12_999 + DefaultOffset;
        public const int TestingStart = 13_000 + DefaultOffset;
        public const int TestingEnd = 13_999 + DefaultOffset;

        public static readonly DataDictionary s_All = new DataDictionary()
        {
                { Min, nameof(Min) },
                { WorldInitStart, nameof(WorldInitStart) },
                { WorldInitEnd, nameof(WorldInitEnd) },
                { TimeSourcesStart, nameof(TimeSourcesStart) },
                { TimeSourcesEnd, nameof(TimeSourcesEnd) },
                { DirectInputStart, nameof(DirectInputStart) },
                { DirectInputEnd, nameof(DirectInputEnd) },
                { DefaultStart, nameof(DefaultStart) },
                { DefaultEnd, nameof(DefaultEnd) },
                { VehicleMotionStart, nameof(VehicleMotionStart) },
                { VehicleMotionEnd, nameof(VehicleMotionEnd) },
                { PlayerMotionStart, nameof(PlayerMotionStart) },
                { PlayerMotionEnd, nameof(PlayerMotionEnd) },
                { WeaponsStart, nameof(WeaponsStart) },
                { WeaponsEnd, nameof(WeaponsEnd) },
                { UiStart, nameof(UiStart) },
                { UiEnd, nameof(UiEnd) },
                { AudioStart, nameof(AudioStart) },
                { AudioEnd, nameof(AudioEnd) },
                { RecordingStart, nameof(RecordingStart) },
                { RecordingEnd, nameof(RecordingEnd) },
                { TestingStart, nameof(TestingStart) },
                { TestingEnd, nameof(TestingEnd) },
                { Max, nameof(Max) },
        };
    }
}
