using System;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Muxers
{
    internal interface IMuxerRegistration : IDisposable
    {
        bool IsValid(FilePath against);
    }
}