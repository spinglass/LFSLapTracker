using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSLapTracker
{
    struct LapTimeDelta
    {
        public static LapTimeDelta Invalid = new LapTimeDelta(double.MaxValue);

        public bool IsValid { get { return m_Seconds != double.MaxValue; } }
        public bool IsNegative { get { return m_Seconds < 0.0; } }
        public bool IsZero { get { return m_Seconds == 0.0; } }
        public bool IsPositive { get { return m_Seconds > 0.0; } }

        public LapTimeDelta(double seconds)
        {
            m_Seconds = seconds;
        }

        public override string ToString()
        {
            string str;
            TimeSpan time = TimeSpan.FromSeconds(Math.Abs(m_Seconds));
            if (time.Minutes > 0)
            {
                str = string.Format("{0}:{1:00}.{2:00}", time.Minutes, time.Seconds, time.Milliseconds / 10);
            }
            else
            {
                str = string.Format("{0}.{1:00}", time.Seconds, time.Milliseconds / 10);
            }
            string sign = IsNegative ? "-" : "+";
            return sign + str;
        }

        private double m_Seconds;
    }
}
