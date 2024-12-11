using VRC.SDK3.Data;

namespace TLP.UdonUtils.Runtime
{
    public static class TlpExecutionOrder
    {
        // Minimum allowed by VRChat
        public const int Min = int.MinValue + 1_000_000;

        public const int DefaultOffset = 150_000;
        public const int SectionWidth = 999;
        public const int WorldInitStart = -3_000 + DefaultOffset;
        public const int WorldInitEnd = WorldInitStart + SectionWidth;

        public const int TimeSourcesStart = WorldInitEnd + 1;
        public const int TimeSourcesEnd = TimeSourcesStart + SectionWidth;

        public const int DirectInputStart = TimeSourcesEnd + 1;
        public const int DirectInputEnd = DirectInputStart + SectionWidth;

        public const int DefaultStart = DirectInputEnd + 1;
        public const int DefaultEnd = DefaultStart + SectionWidth;

        public const int VehicleMotionStart = DefaultEnd + 1;
        public const int VehicleMotionEnd = VehicleMotionStart + SectionWidth;

        public const int PlayerMotionStart = VehicleMotionEnd + 1;
        public const int PlayerMotionEnd = PlayerMotionStart + SectionWidth;

        public const int WeaponsStart = PlayerMotionEnd + 1;
        public const int WeaponsEnd = WeaponsStart + SectionWidth;

        public const int CameraStart = WeaponsEnd + 1;
        public const int CameraEnd = CameraStart + SectionWidth;

        public const int UiStart = CameraEnd + 1;
        public const int UiEnd = UiStart + SectionWidth;

        public const int AudioStart = UiEnd + 1;
        public const int AudioEnd = AudioStart + SectionWidth;

        public const int RecordingStart = AudioEnd + 1;
        public const int RecordingEnd = RecordingStart + SectionWidth;

        public const int TestingStart = RecordingEnd + 1;
        public const int TestingEnd = TestingStart + SectionWidth;

        public const int Max = TestingEnd + 1;


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
                { CameraStart, nameof(CameraStart) },
                { CameraEnd, nameof(CameraEnd) },
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