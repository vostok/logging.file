using System.IO;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;

namespace Vostok.Logging.File.Configuration
{
    [PublicAPI]
    public enum WriteMode
    {
        /// <summary>
        /// <para>Log methods always return instantly.</para>
        /// <para>All the necessary I/O (writing to file) happens in the background.</para>
        /// </summary>
        Asynchronous,

        /// <summary>
        /// <para>Same as <see cref="Asynchronous"/> until events buffer is full.</para>
        /// <para>Otherwise, waits until there is some space in events buffer.</para>
        /// </summary>
        AsynchronousUntilEventsBufferIsFull,
        
        /// <summary>
        /// <para>Log methods return after <see cref="TextWriter.Flush"/> is called.</para>
        /// </summary>
        Synchronous
    }
}