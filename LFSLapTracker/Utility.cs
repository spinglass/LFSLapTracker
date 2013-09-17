using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSLapTracker
{
    static class Utility
    {
        public static string ToTimeString(TimeSpan time)
        {
            string str;
            if (time.Hours > 0)
            {
                str = string.Format("{0:}:{1:00}:{2:00}.{3:00}", time.Hours, time.Minutes, time.Seconds, time.Milliseconds / 10);
            }
            else if (time.Minutes > 0)
            {
                str = string.Format("{0}:{1:00}.{2:00}", time.Minutes, time.Seconds, time.Milliseconds / 10);
            }
            else
            {
                str = string.Format("{0}.{1:00}", time.Seconds, time.Milliseconds / 10);
            }
            return str;
        }

        public static string ToTimeString(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return ToTimeString(time);
        }

        public static string ToTimeDeltaString(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Math.Abs(seconds));
            string timeStr = ToTimeString(time);
            string sign = (seconds < 0.0) ? "-" : "+";
            return sign + timeStr;
        }
    }
}
