using System;
using System.Diagnostics;

namespace Core.Time
{
    public class TimeHelper
    {
        // ==================================================================================================
        // STATIC's
        // ==================================================================================================
        private static readonly long s_lStartTime = Stopwatch.GetTimestamp();
        private static readonly double s_dFrequency =
          1.0 / (double)Stopwatch.Frequency;

        // ==================================================================================================
        // PROPERTY'ies
        // ==================================================================================================

        /// <summary>
        /// Time represented as elapsed seconds.
        /// </summary>
        public static double Time
        {
            get
            {
                long lDiff = Stopwatch.GetTimestamp() - s_lStartTime;
                return (double)lDiff * s_dFrequency;
            }
        }

        /// <summary>
        /// Time represented as elapsed seconds.
        /// </summary>
        public static long Tick
        {
            get
            {
                return Stopwatch.GetTimestamp() /*- s_lStartTime*/;
            }
        }
    }
}
