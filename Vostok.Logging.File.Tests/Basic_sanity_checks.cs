using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

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

                using (var log = new FileLog(new FileLogSettings {FilePath = logName}))
                {
                    log.Info("Hello, world!");
                    log.Info("Hello, world!");
                    log.Info("Hello, world!");
                }

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

                using (var log = new FileLog(
                    new FileLogSettings
                    {
                        FilePath = logName,
                        RollingStrategy = new RollingStrategyOptions
                        {
                            Type = RollingStrategyType.BySize,
                            MaxSize = 1024,
                            MaxFiles = 3
                        },
                        RollingUpdateCooldown = TimeSpan.FromMilliseconds(10)
                    }))
                {
                    for (int i = 0; i < 100; i++)
                    {
                        log.Info("Hello, world!");
                        Thread.Sleep(10);
                    }
                }

                var files = new FileSystem().GetFilesByPrefix(logName).ToArray();
                files.Length.Should().Be(3);

                foreach (var file in files)
                {
                    Console.WriteLine($"{file}: {new FileInfo(file.NormalizedPath).Length}");
                }

                Console.WriteLine(System.IO.File.ReadAllText(files.Last().NormalizedPath));
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
                            var logs = new List<FileLog>();

                            latch.Signal();
                            latch.Wait();
                            for (int x = 0; x < 100; x++)
                            {
                                var log = new FileLog(new FileLogSettings { FilePath = logName });
                                log.Info("Hey from {writer:'writer '#00}!", i);
                                logs.Add(log);
                            }

                            foreach (var log in logs)
                                log.Dispose();
                        })).ToArray();

                latch.Wait();
                Task.WaitAll(writers);

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

                using (var log = new FileLog(
                    new FileLogSettings
                    {
                        FilePath = logName,
                        RollingStrategy = new RollingStrategyOptions
                        {
                            Type = RollingStrategyType.BySize,
                            MaxSize = 1024
                        },
                        RollingUpdateCooldown = TimeSpan.FromMilliseconds(10)
                    }))
                {
                    for (int i = 0; i < 100; i++)
                    {
                        log.Info("Hello, world!");
                        Thread.Sleep(10);
                    }
                }

                var files = new FileSystem().GetFilesByPrefix(logName).ToArray();
                files.Length.Should().Be(5);

                foreach (var file in files)
                {
                    Console.WriteLine($"{file}: {new FileInfo(file.NormalizedPath).Length}");
                }

                Console.WriteLine(System.IO.File.ReadAllText(files.Last().NormalizedPath));
            }
        }

        [Test]
        public void Should_roll_by_size_with_extension()
        {
            using (var folder = new TemporaryFolder())
            {
                var logName = folder.GetFileName("log.txt");

                using (var log = new FileLog(
                    new FileLogSettings
                    {
                        FilePath = logName,
                        RollingStrategy = new RollingStrategyOptions
                        {
                            Type = RollingStrategyType.BySize,
                            MaxSize = 1024
                        },
                        RollingUpdateCooldown = TimeSpan.FromMilliseconds(10)
                    }))
                {
                    for (int i = 0; i < 100; i++)
                    {
                        log.Info("Hello, world!");
                        Thread.Sleep(10);
                    }
                }

                var files = new FileSystem().GetFilesByPrefix(logName.Substring(0, logName.Length - 4)).ToArray();
                files.Length.Should().Be(5);

                foreach (var file in files)
                {
                    Console.WriteLine($"{file}: {new FileInfo(file.NormalizedPath).Length}");
                }

                Console.WriteLine(System.IO.File.ReadAllText(files.Last().NormalizedPath));
            }
        }

        [Test]
        public void Should_roll_by_time()
        {
            using (var folder = new TemporaryFolder())
            {
                var logName = folder.GetFileName("log");

                using (var log = new FileLog(
                    new FileLogSettings
                    {
                        FilePath = logName,
                        RollingStrategy = new RollingStrategyOptions
                        {
                            Type = RollingStrategyType.ByTime,
                            Period = RollingPeriod.Second
                        },
                        RollingUpdateCooldown = TimeSpan.FromMilliseconds(10)
                    }))
                {
                    for (int i = 0; i < 100; i++)
                    {
                        log.Info("Hello, world!");
                        Thread.Sleep(50);
                    }
                }

                var files = new FileSystem().GetFilesByPrefix(logName).ToArray();
                files.Length.Should().BeGreaterThan(1);

                foreach (var file in files)
                {
                    Console.WriteLine($"{file}: {new FileInfo(file.NormalizedPath).Length}");
                }

                Console.WriteLine(System.IO.File.ReadAllText(files.Last().NormalizedPath));
            }
        }

        [Test]
        public void Should_roll_by_time_and_size()
        {
            using (var folder = new TemporaryFolder())
            {
                var logName = folder.GetFileName("log");

                using (var log = new FileLog(
                    new FileLogSettings
                    {
                        FilePath = logName,
                        RollingStrategy = new RollingStrategyOptions
                        {
                            Type = RollingStrategyType.Hybrid,
                            Period = RollingPeriod.Second,
                            MaxSize = 300
                        },
                        RollingUpdateCooldown = TimeSpan.FromMilliseconds(10)
                    }))
                {
                    for (int i = 0; i < 100; i++)
                    {
                        log.Info("Hello, world!");
                        Thread.Sleep(100);
                    }
                }

                var files = new FileSystem().GetFilesByPrefix(logName).ToArray();
                files.Length.Should().BeGreaterThan(1);

                foreach (var file in files)
                {
                    Console.WriteLine($"{file}: {new FileInfo(file.NormalizedPath).Length}");
                }

                Console.WriteLine(System.IO.File.ReadAllText(files.Last().NormalizedPath));
            }
        }

        [Test]
        public void Should_create_directories_leading_to_log_file_path_if_needed()
        {
            using (var folder = new TemporaryFolder())
            {
                var logName = Path.Combine(folder.Name, "dir1", "dir2", "log");

                using (var log = new FileLog(new FileLogSettings { FilePath = logName }))
                {
                    log.Info("Hello, world!");
                    log.Info("Hello, world!");
                    log.Info("Hello, world!");
                }

                System.IO.File.Exists(logName).Should().BeTrue();

                Console.WriteLine(System.IO.File.ReadAllText(logName));
            }
        }
    }
}