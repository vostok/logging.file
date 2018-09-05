using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vostok.Logging.File.EventsWriting
{
    internal interface IEventsWriterProvider : IDisposable
    {
        [ItemCanBeNull]
        Task<IEventsWriter> ObtainWriterAsync();
    }
}