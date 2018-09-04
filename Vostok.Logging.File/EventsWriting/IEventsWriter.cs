using System;

namespace Vostok.Logging.File.EventsWriting
{
    internal interface IEventsWriter : IDisposable
    {
        void WriteEvents(LogEventInfo[] temporaryBuffer, int eventsCount);
    }
}