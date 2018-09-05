using System;
using JetBrains.Annotations;

namespace Vostok.Logging.File
{
    [PublicAPI]
    public class FileLogException : Exception
    {
        public FileLogException(string message)
            : base(message)
        {
        }

        public FileLogException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}