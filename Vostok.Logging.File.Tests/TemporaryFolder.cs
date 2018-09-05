using System;
using System.IO;
using System.Threading;

namespace Vostok.Logging.File.Tests
{
    internal class TemporaryFolder : IDisposable
    {
        private readonly DirectoryInfo directoryInfo;

        public string Name => directoryInfo.FullName;

        public TemporaryFolder()
        {
            directoryInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString()));
            directoryInfo.Create();
        }

        public string GetFileName(string file) => Path.Combine(Name, file);

        public void Dispose()
        {
            for (var i = 0; ; i++)
            {
                try
                {
                    directoryInfo.Delete(true);
                    break;
                }
                catch
                {
                    if (i == 5)
                        throw;
                    Thread.Sleep(100);
                }
            }
        }
    }
}