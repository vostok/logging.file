using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vostok.Logging.File.EventsWriting
{
    internal interface IEventsWriterProvider : IDisposable
    {
        /// <summary>
        /// <para>Attempts to obtain an <see cref="IEventsWriter"/> instance to write <see cref="LogEventInfo"/>s with.</para>
        /// <para>Returns <c>null</c> on any failure, such as file opening error.</para>
        /// <para>May wait for a cooldown provided by <see cref="ICooldownController"/> before trying to open a writer.</para>
        /// </summary>
        [ItemCanBeNull]
        Task<IEventsWriter> ObtainWriterAsync();
    }
}
