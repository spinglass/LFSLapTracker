using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSLapTracker
{
    struct LapTime
    {
        public bool IsValid { get { return (0.0 < m_Seconds && m_Seconds < c_MaxTime); } }

        public LapTime(double seconds)
        {
            m_Seconds = seconds;
        }

        public static bool operator <(LapTime t1, LapTime t2)
        {
            return (t1.m_Seconds < t2.m_Seconds);
        }

        public static bool operator >(LapTime t1, LapTime t2)
        {
            return (t1.m_Seconds > t2.m_Seconds);
        }

        public static LapTimeDelta operator -(LapTime t1, LapTime t2)
        {
            return new LapTimeDelta(t1.m_Seconds - t2.m_Seconds);
        }

        public override string ToString()
        {
            string str;
            TimeSpan time = TimeSpan.FromSeconds(m_Seconds);
            if (time.Minutes > 0)
            {
                str = string.Format("{0}:{1:00}.{2:00}", time.Minutes, time.Seconds, time.Milliseconds / 10);
            }
            else
            {
                str = string.Format("{0}.{1:00}", time.Seconds, time.Milliseconds / 10);
            }
            return str;
        }

        private const double c_MaxTime = 3600.0;
        private double m_Seconds;
    }
}
