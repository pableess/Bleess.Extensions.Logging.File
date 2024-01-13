using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bleess.Extensions.Logging.File.Multiple
{
    public class MultiFileLoggerOptions
    {
        public List<FileLoggerOptions> FileLoggerOptions { get; set; }
    }
}
