using System;
using TLP.UdonUtils.Runtime.Sources;

namespace TLP.UdonUtils.Runtime.Sources.Time
{
    /// <summary>
    /// <see cref="TimeSource"/> that uses the local UTC time to provide
    /// the seconds since 1970/01/01 00:00.000000
    /// </summary>
    public class UtcTimeSource : TimeSource
    {
        private readonly DateTime _referenceTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override float Time() {
            return (float) (DateTime.UtcNow - _referenceTimeUtc).TotalSeconds;
        }

        public override double TimeAsDouble() {
            return (DateTime.UtcNow - _referenceTimeUtc).TotalSeconds;
        }
    }
}

