using System;
using System.IO;
using Vostok.Configuration.Abstractions;
using Vostok.Logging.Core;

namespace Vostok.Logging.FileLog
{
    internal class EventsWriter : IDisposable
    {
        private readonly TextWriter writer;

        public EventsWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        public void WriteEvents(LogEventInfo[] events, int eventsCount)
        {
            for (var i = 0; i < eventsCount; i++)
            {
                try
                {
                    events[i].Settings.ConversionPattern.Render(events[i].Event, writer);
                }
                catch
                {
                    // ignored
                }
            }

            writer.Flush();
        }
        
        public void Dispose() => writer?.Dispose();
    }
}