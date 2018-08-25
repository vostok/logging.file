using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Commons.Primitives;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Tests
{
    [TestFixture]
    internal class Basic_sanity_checks
    {
        [Test]
        public void Should_write_to_file_without_rolling()
        {
            using (var folder = new TemporaryFolder())
            {
                var logName = folder.GetFileName("log");

                var log = new FileLog(new FileLogSettings {FilePath = logName});

                log.Info("Hello, world!");
                log.Info("Hello, world!");
                log.Info("Hello, world!");

                log.Flush();
                log.Close();

                System.IO.File.Exists(logName).Should().BeTrue();

                Console.WriteLine(System.IO.File.ReadAllText(logName));
            }
        }

        [Test]
        public void Should_delete_old_files()
        {
            using (var folder = new TemporaryFolder())
            {
                var logName = folder.GetFileName("log");

                var log = new FileLog(new FileLogSettings
                {
                    FilePath = logName,
                    RollingStrategy = new RollingStrategyOptions
                    {
                        Type = RollingStrategyType.BySize,
                        MaxSize = DataSize.FromKilobytes(1),
                        MaxFiles = 3
                    }
                });

                for (int i = 0; i < 100; i++)
                {
                    log.Info("Hello, world!");
                    Thread.Sleep(10);
                }

                log.Flush();
                log.Close();

                var files = new FileSystem(() => new FileLogSettings()).GetFilesByPrefix(logName);
                files.Length.Should().Be(3);

                foreach (var file in files)
                {
                    Console.WriteLine($"{file}: {new FileInfo(file).Length}");
                }

                Console.WriteLine(System.IO.File.ReadAllText(files.Last()));
            }
        }

        [Test]
        public void Should_write_to_file_from_different_logs_and_threads()
        {
            using (var folder = new TemporaryFolder())
            {
                var logName = folder.GetFileName("log");

                var latch = new CountdownEvent(3);
                var writers = new[] {1, 2, 3}.Select(
                    i => Task.Run(
                        () =>
                        {
                            latch.Signal();
                            latch.Wait();
                            for (int x = 0; x < 100; x++)
                                new FileLog(new FileLogSettings { FilePath = logName }).Info("Hey from {writer:'writer '#00}!", i);

                        })).ToArray();

                latch.Wait();
                Task.WaitAll(writers);

                new FileLog(new FileLogSettings { FilePath = logName }).Flush();
                new FileLog(new FileLogSettings { FilePath = logName }).Close();

                System.IO.File.Exists(logName).Should().BeTrue();

                Console.WriteLine(System.IO.File.ReadAllText(logName));
            }
        }

        [Test]
        public void Should_roll_by_size()
        {
            using (var folder = new TemporaryFolder())
            {
                var logName = folder.GetFileName("log");

                var log = new FileLog(new FileLogSettings { FilePath = logName, RollingStrategy = new RollingStrategyOptions
                {
                    Type = RollingStrategyType.BySize,
                    MaxSize = DataSize.FromKilobytes(1)
                }});

                for (int i = 0; i < 100; i++)
                {
                    log.Info("Hello, world!");
                    Thread.Sleep(10);
                }

                log.Flush();
                log.Close();

                var files = new FileSystem(() => new FileLogSettings()).GetFilesByPrefix(logName);
                files.Length.Should().Be(5);

                foreach (var file in files)
                {
                    Console.WriteLine($"{file}: {new FileInfo(file).Length}");
                }

                Console.WriteLine(System.IO.File.ReadAllText(files.Last()));
            }
        }

        [Test]
        public void Should_roll_by_time()
        {
            using (var folder = new TemporaryFolder())
            {
                var logName = folder.GetFileName("log");

                var log = new FileLog(new FileLogSettings
                {
                    FilePath = logName,
                    RollingStrategy = new RollingStrategyOptions
                    {
                        Type = RollingStrategyType.ByTime,
                        Period = 1.Seconds()
                    }
                });

                for (int i = 0; i < 100; i++)
                {
                    log.Info("Hello, world!");
                    Thread.Sleep(50);
                }

                log.Flush();
                log.Close();

                var files = new FileSystem(() => new FileLogSettings()).GetFilesByPrefix(logName);
                files.Length.Should().BeGreaterThan(1);

                foreach (var file in files)
                {
                    Console.WriteLine($"{file}: {new FileInfo(file).Length}");
                }

                Console.WriteLine(System.IO.File.ReadAllText(files.Last()));
            }
        }

        [Test]
        public void Should_roll_by_time_and_size()
        {
            using (var folder = new TemporaryFolder())
            {
                var logName = folder.GetFileName("log");

                var log = new FileLog(new FileLogSettings
                {
                    FilePath = logName,
                    RollingStrategy = new RollingStrategyOptions
                    {
                        Type = RollingStrategyType.Hybrid,
                        Period = 1.Seconds(),
                        MaxSize = DataSize.FromBytes(300)
                    }
                });

                for (int i = 0; i < 100; i++)
                {
                    log.Info("Hello, world!");
                    Thread.Sleep(100);
                }

                //log.Flush();
                Thread.Sleep(1000);
                log.Close();

                var files = new FileSystem(() => new FileLogSettings()).GetFilesByPrefix(logName);
                files.Length.Should().BeGreaterThan(1);

                foreach (var file in files)
                {
                    Console.WriteLine($"{file}: {new FileInfo(file).Length}");
                }

                Console.WriteLine(System.IO.File.ReadAllText(files.Last()));
            }
        }

        private class TemporaryFolder : IDisposable
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
                for (var i = 0;; i++)
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
}