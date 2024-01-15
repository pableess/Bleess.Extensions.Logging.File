using System;
using System.Collections.Generic;
using System.Text;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Wrapper for a named sub logger of a composite logger
    /// </summary>
    public class SubLoggerRegistration : ISubLoggerRegistration
    {
        /// <summary>
        /// Creates the sub logger name
        /// </summary>
        /// <param name="configName"></param>
        public SubLoggerRegistration(string configName)
        {
            this.Name = configName;
        }

        /// <inheritdoc/>
        // The name of the sub logger
        public string Name {  get; private set; }
    }
}
