using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NET8_0_OR_GREATER

namespace Bleess.Extensions.Logging.File.Multiple;

public static class MultipleFileLoggerBuilderExtensions
{
    ///// <summary>
    ///// Add support for multiple file loggers defined in configuration.  
    ///// File logger configuration sections need to Begin with "File" prefix
    ///// </summary>
    ///// <param name="builder"></param>
    ///// <returns></returns>
    //public static ILoggingBuilder AddFiles(this ILoggingBuilder builder) 
    //{
    //    builder.AddConfiguration();

    //}
}

#endif
