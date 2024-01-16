using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Log file rolling interval
    /// </summary>
    public enum RollingInterval
    {
        /// <summary>
        /// The log file will never roll; no time period information will be appended to the log file name.
        /// </summary>
        Infinite,

        /// <summary>
        /// Roll every year. Filenames will have a four-digit year appended in the pattern <code>yyyy</code>.
        /// </summary>
        Year,

        /// <summary>
        /// Roll every calendar month. Filenames will have <code>yyyyMM</code> appended.
        /// </summary>
        Month,

        /// <summary>
        /// Roll every day. Filenames will have <code>yyyyMMdd</code> appended.
        /// </summary>
        Day,

        /// <summary>
        /// Roll every hour. Filenames will have <code>yyyyMMddHH</code> appended.
        /// </summary>
        Hour,

        /// <summary>
        /// Roll every minute. Filenames will have <code>yyyyMMddHHmm</code> appended.
        /// </summary>
        Minute
    }
}
