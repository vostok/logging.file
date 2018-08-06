using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.FileLog.Configuration;
using Vostok.Logging.Formatting;

namespace Vostok.Logging.FileLog.Tests
{
    [TestFixture]
    internal class FileLog_Tests
    {
        [Test]
        public void FileLog_should_work_correctly_for_default_settings()
        {
            var messages = new[] {"Hello, World 1", "Hello, World 2"};

            log.Info(messages[0]);
            log.Info(messages[1]);
            WaitForOperationCanceled();

            createdFiles.Add(settings.FilePath);

            ReadAllLines(settings.FilePath).Should().BeEquivalentTo(messages);
        }
        [Test]
        public void FileLog_should_switch_logfile_if_FilePath_was_changed()
        {
            var messages = new[] { "Hello, World 1", "Hello, World 2" };

            log.Info(messages[0]);
            WaitForOperationCanceled();

            var oldFilePath = settings.FilePath;
            UpdateSettings(s => s.FilePath = $"{Guid.NewGuid().ToString().Substring(0, 8)}.log");

            log.Info(messages[1]);
            WaitForOperationCanceled();

            createdFiles.Add(oldFilePath);
            createdFiles.Add(settings.FilePath);

            ReadAllLines(oldFilePath).Should().BeEquivalentTo(messages[0]);
            ReadAllLines(settings.FilePath).Should().BeEquivalentTo(messages[1]);
        }

        [Test]
        public void FileLog_should_rewrite_logfile_if_configured_so()
        {
            var messages = new[] { "Hello, World 1", "Hello, World 2" };

            log.Info(messages[0]);
            WaitForOperationCanceled();

            UpdateSettings(s => s.FileOpenMode = FileOpenMode.Rewrite);

            log.Info(messages[1]);
            WaitForOperationCanceled();

            createdFiles.Add(settings.FilePath);

            ReadAllLines(settings.FilePath).Should().BeEquivalentTo(messages[1]);
        }

        [Test]
        public void FileLog_should_use_date_in_logfile_name_for_ByTime_rolling_strategy()
        {
            var messages = new[] { "Hello, World 1", "Hello, World 2" };

            log.Info(messages[0]);
            WaitForOperationCanceled();

            UpdateSettings(s => s.RollingStrategy = new FileLogSettings.RollingStrategyOptions
            {
                Type = RollingStrategyType.ByTime,
                Period = 1.Days()
            });

            log.Info(messages[1]);
            WaitForOperationCanceled();

            createdFiles.Add(settings.FilePath);
            createdFiles.Add(AddDate(settings.FilePath));

            ReadAllLines(settings.FilePath).Should().BeEquivalentTo(messages[0]);
            ReadAllLines(AddDate(settings.FilePath)).Should().BeEquivalentTo(messages[1]);
        }

        [Test]
        public void FileLog_should_change_notes_format_if_ConversionPattern_was_updated()
        {
            var messages = new[] { "Hello, World 1", "Hello, World 2" };

            log.Info(messages[0], new { trace = 134 });
            WaitForOperationCanceled();

            UpdateSettings(s => s.OutputTemplate = OutputTemplate.Parse("%l %p(trace) %m%n"));

            log.Info(messages[1], new { trace = 134 });
            WaitForOperationCanceled();

            createdFiles.Add(settings.FilePath);

            ReadAllLines(settings.FilePath).Should().BeEquivalentTo(messages[0], $"Info 134 {messages[1]}");
        }

        [Test]
        public void FileLog_should_switch_notes_encoding_if_Encoding_was_updated()
        {
            var messages = new[] { "Hello, World 1{0}", "Hello, World 2{0}" };
            
            log.Info(messages[0], "€®");
            WaitForOperationCanceled();

            UpdateSettings(s => s.Encoding = Encoding.ASCII);

            log.Info(messages[1], "€®");
            WaitForOperationCanceled();

            createdFiles.Add(settings.FilePath);

            ReadAllLines(settings.FilePath).Should().BeEquivalentTo(string.Format(messages[0], "€®"), string.Format(messages[1], "??"));
        }

        [SetUp]
        public void SetUp()
        {
            settings = new FileLogSettings
            {
                FilePath = $"{Guid.NewGuid().ToString().Substring(0, 8)}.log",
                OutputTemplate = OutputTemplate.Parse("%m%n"),
                Encoding = Encoding.UTF8
            };

            UpdateSettings(settings);
        }

        [TearDown]
        public void TearDown()
        {
            UpdateSettings(TempFileSettings);
            log.Info(string.Empty);
            createdFiles.ForEach(DeleteFile);
            createdFiles.Clear();
        }

        private static FileLogSettings TempFileSettings => new FileLogSettings
        {
            FilePath = "temp",
            OutputTemplate = OutputTemplate.Parse(string.Empty)
        };

        private static void DeleteFile(string fileName)
        {
            while (true)
            {
                try
                {
                    if (System.IO.File.Exists(fileName))
                        System.IO.File.Delete(fileName);

                    break;
                }
                catch (Exception)
                {
                    WaitForOperationCanceled();
                }
            }
        }

        private void UpdateSettings(FileLogSettings settingsPatch)
        {
            settings = settingsPatch;
            log = new FileLog(settingsPatch);
            WaitForOperationCanceled();
        }

        private void UpdateSettings(Action<FileLogSettings> settingsPatch)
        {
            var copy = new FileLogSettings
            {
                FilePath = settings.FilePath,
                OutputTemplate = settings.OutputTemplate,
                RollingStrategy = settings.RollingStrategy,
                FileOpenMode = settings.FileOpenMode,
                Encoding = settings.Encoding,
                EventsQueueCapacity = settings.EventsQueueCapacity,
                EnabledLogLevels = settings.EnabledLogLevels
            };

            settingsPatch(copy);

            UpdateSettings(copy);
        }

        private static void WaitForOperationCanceled()
        {
            Thread.Sleep(300);
        }

        private static string AddDate(string fileName)
        {
            return $"{fileName}{DateTimeOffset.UtcNow.Date:yyyy.MM.dd}";
        }

        private static IEnumerable<string> ReadAllLines(string fileName)
        {
            using (var file = System.IO.File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(file))
            {
                return reader.ReadToEnd().Split(Environment.NewLine.ToArray()).Where(s => !string.IsNullOrEmpty(s));
            }
        }

        private FileLogSettings settings;

        private FileLog log;
        private readonly List<string> createdFiles = new List<string>(2);
    }
}