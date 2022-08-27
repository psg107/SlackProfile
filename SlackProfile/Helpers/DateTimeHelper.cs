using System;

namespace SlackProfile.Helpers
{
    public class DateTimeHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static long GetTodayEndUnixTimestamp()
        {
            var endDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59, DateTimeKind.Local);
            var endDateTimeOffset = new DateTimeOffset(endDateTime);
            
            var unixTimestamp = endDateTimeOffset.ToUnixTimeSeconds();

            return unixTimestamp;
        }
    }
}
