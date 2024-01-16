using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bleess.Extensions.Logging.File;

internal static class RollingIntervalExtensions
{
        public static string GetFormat(this RollingInterval interval)
        {
            return interval switch
            {
                RollingInterval.Infinite => string.Empty,
                RollingInterval.Year => "yyyy",
                RollingInterval.Month => "yyyyMM",
                RollingInterval.Day => "yyyyMMdd",
                RollingInterval.Hour => "yyyyMMddHH",
                RollingInterval.Minute => "yyyyMMddHHmm",
                _ => throw new ArgumentException("Invalid rolling interval"),
            };
        }

        public static string ToFormattedString(this DateTime? dateTime, RollingInterval rollingInterval) => dateTime?.ToString(rollingInterval.GetFormat());

        public static DateTime? Truncate(this DateTime dateTime, RollingInterval interval)
        {
            return interval switch
            {
                RollingInterval.Infinite => null,
                RollingInterval.Year => (DateTime?)new DateTime(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind),
                RollingInterval.Month => (DateTime?)new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind),
                RollingInterval.Day => (DateTime?)new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Kind),
                RollingInterval.Hour => (DateTime?)new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, dateTime.Kind),
                RollingInterval.Minute => (DateTime?)new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, dateTime.Kind),
                _ => throw new ArgumentException("Invalid rolling interval"),
            };
        }
}
