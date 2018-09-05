using System;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.MuxersNew
{
    internal interface IMuxerRegistration : IDisposable
    {
        bool IsValid(FilePath against);
    }
}