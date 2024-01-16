using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Special settings/options for adding filter rules for logger defined under a composite logging provider
    /// </summary>
    public record class CompositeLoggerFilterOptions
    {
        /// <summary>
        ///   Gets the collection of Microsoft.Extensions.Logging.LoggerFilterRule used for
        ///     filtering log messages.
        /// </summary>
        public IList<LoggerFilterRule> Rules { get; } = new List<LoggerFilterRule>();
    }
}
