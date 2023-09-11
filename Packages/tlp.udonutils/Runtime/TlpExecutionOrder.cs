namespace TLP.UdonUtils
{
    public static class TlpExecutionOrder
    {
        public const int Min = int.MinValue + 1_000_000;
        public const int Max = AudioEnd + 1;

        public const int DefaultOffset = 150_000;
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
    }
}