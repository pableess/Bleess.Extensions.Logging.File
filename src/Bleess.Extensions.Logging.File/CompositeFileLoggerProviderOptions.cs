using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Options for defining composite file logger provider
    /// </summary>
    public class CompositeFileLoggerProviderOptions
    {
        /// <summary>
        /// Gets or sets the names of all the file logging providers for the composite provider
        /// </summary>
        public string[] Providers { get; set; }
    }
}
