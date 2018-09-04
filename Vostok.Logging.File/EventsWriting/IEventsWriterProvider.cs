using System;

namespace Vostok.Logging.File.EventsWriting
{
    internal interface IEventsWriterProvider : IDisposable
    {
        bool IsHealthy { get; }

        IEventsWriter ObtainWriter();
    }
}