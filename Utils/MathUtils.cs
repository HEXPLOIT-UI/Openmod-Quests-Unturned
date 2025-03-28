using System;

namespace Quests.Utils
{
    public class MathUtils
    {
        // scales players progress value in a quest to a value that fits the progress bar model
        public static double MapToRange(double value, double minInput, double maxInput)
        {
            return Math.Min(100, Math.Max(0, (value - minInput) / (maxInput - minInput) * 100));
        }

        public static string getFormatedTime(long millis)
        {
            int totalSecs = (int)((DateTimeOffset.Now.ToUnixTimeMilliseconds() > millis ? 0L : millis - DateTimeOffset.Now.ToUnixTimeMilliseconds()) / 1000);
            int hours = (int)((long)totalSecs % 86400L) / 3600;
            int minutes = (int)((long)totalSecs % 3600L) / 60;
            int seconds = (int)((long)totalSecs % 60) / 1;
            return hours + "h:" + minutes + "m:" + seconds + "s";
        }
    }
}
