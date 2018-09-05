using System;

namespace Vostok.Logging.File.Helpers
{
    internal static class SafeConsole
    {
        public static void ReportError(string message, Exception error)
        {
            try
            {
                Console.Out.WriteLine("[FileLog] " + message);
                Console.Out.WriteLine(error);
            }
            catch
            {
                // ignored
            }
        }
    }
}