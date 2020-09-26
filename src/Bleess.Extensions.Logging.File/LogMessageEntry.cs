using System;
using System.Collections.Generic;
using System.Text;

namespace Bleess.Extensions.Logging.File
{
    internal readonly struct LogMessageEntry
    {
        public LogMessageEntry(string message)
        {
            Message = message;
        }

        public readonly string Message;
    }
}
