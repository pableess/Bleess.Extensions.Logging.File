using System;
using System.Collections.Generic;
using System.Text;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Interface for ISubLoggerRegistration
    /// </summary>
    public interface ISubLoggerRegistration
    {
        /// <summary>
        /// Gets the sub logger provider name
        /// </summary>
        public string Name { get; }
    }
}
