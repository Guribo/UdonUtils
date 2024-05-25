using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Extensions
{
    public static class NetworkUtils
    {
        public static float Latency(this DeserializationResult deserializationResult) {
            if (deserializationResult.receiveTime > deserializationResult.sendTime) {
                return deserializationResult.receiveTime - deserializationResult.sendTime;
            }

            return 0f;
        }
    }
}