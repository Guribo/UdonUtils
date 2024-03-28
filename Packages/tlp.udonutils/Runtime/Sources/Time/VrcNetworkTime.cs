using VRC.SDKBase;

namespace TLP.UdonUtils.Sources.Time
{
    /// <summary>
    /// Implementation of <see cref="TimeSource"/>
    /// using <see cref="Networking.GetServerTimeInSeconds"/>
    /// </summary>
    public class VrcNetworkTime : TimeSource
    {
        /// <returns><see cref="Networking.GetServerTimeInSeconds"/></returns>
        public override float Time() {
            return (float)Networking.GetServerTimeInSeconds();
        }

        /// <returns><see cref="Networking.GetServerTimeInSeconds"/></returns>
        public override double TimeAsDouble() {
            return Networking.GetServerTimeInSeconds();
        }
    }
}
