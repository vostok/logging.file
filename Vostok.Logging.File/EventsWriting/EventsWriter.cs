using System;
using System.IO;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.Formatting;

namespace Vostok.Logging.File.EventsWriting
{
    internal class EventsWriter : IEventsWriter
    {
        private readonly TextWriter writer;

        public EventsWriter(TextWriter writer) => this.writer = writer;

        public void WriteEvents(LogEventInfo[] events, int eventsCount)
        {
            for (var i = 0; i < eventsCount; i++)
            {
                try
                {
                    var template = events[i].Settings.OutputTemplate;
                    var formatProvider = events[i].Settings.FormatProvider;

                    LogEventFormatter.Format(events[i].Event, writer, template, formatProvider);
                }
                catch (Exception error)
                {
                    SafeConsole.ReportError("Failed to write a log event:", error);
                }
            }

            writer.Flush();
        }

        public void Dispose() => writer?.Dispose();
    }
}