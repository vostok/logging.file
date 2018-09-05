using System;
using System.Threading.Tasks;

namespace Vostok.Logging.File.EventsWriting
{
    internal interface IEventsWriterProvider : IDisposable
    {
        Task<IEventsWriter> ObtainWriterAsync();
    }
}