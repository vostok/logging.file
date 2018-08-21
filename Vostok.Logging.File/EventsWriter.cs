using System;
using System.IO;
using Vostok.Logging.Formatting;

namespace Vostok.Logging.File
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
                    var template = events[i].Settings.OutputTemplate;

                    LogEventFormatter.Format(events[i].Event, writer, template);
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