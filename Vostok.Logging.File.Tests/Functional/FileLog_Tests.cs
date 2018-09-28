using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Tests.Functional
{
    [TestFixture]
    internal class FileLog_Tests: FileLogFunctionalTestsBase
    {
        [Test]
        public void Should_write_to_file_without_rolling()
        {
            var logName = Folder.GetFileName("log");

            var messages = GenerateMessages(0, 3);
            
            using (var log = new FileLog(new FileLogSettings {FilePath = logName}))
            {
                WriteMessagesWithTimeout(log, messages, 0);
            }

            System.IO.File.Exists(logName).Should().BeTrue();

            ShouldContainMessages(logName, messages);
        }
        
        [Test]
        public void Should_write_to_file_from_different_logs_and_threads()
        {
            const int writersCount = 3;
            const int messagesCount = 100;

            var writerMessages = Enumerable
                .Range(0, writersCount)
                .Select(
                    writerIndex =>
                        Enumerable
                            .Range(0, messagesCount)
                            .Select(msgIndex => $"Writer {writerIndex}, message {msgIndex}")
                            .ToArray()
                )
                .ToArray();
            
            var logName = Folder.GetFileName("log");

            var latch = new CountdownEvent(writersCount);
            var writers = Enumerable.Range(0, writersCount).Select(
                writerIndex => Task.Run(
                    () =>
                    {
                        var logs = new List<FileLog>();

                        latch.Signal();
                        latch.Wait();
                        for (var messageIndex = 0; messageIndex < messagesCount; messageIndex++)
                        {
                            var log = new FileLog(new FileLogSettings { FilePath = logName });
                            log.Info(writerMessages[writerIndex][messageIndex]);
                            logs.Add(log);
                        }

                        foreach (var log in logs)
                            log.Dispose();
                    })).ToArray();

            latch.Wait();
            Task.WaitAll(writers);

            System.IO.File.Exists(logName).Should().BeTrue();
            ShouldContainMessages(logName, writerMessages.SelectMany(messages => messages));
        }
        
        [Test]
        public void Should_write_events_with_enabled_levels_only()
        {
            var logName = Folder.GetFileName("log");

            using (var log = new FileLog(new FileLogSettings
            {
                FilePath = logName,
                EnabledLogLevels = new []{LogLevel.Error, LogLevel.Fatal}
            }))
            {
                log.Fatal("Fatal");
                log.Error("Error");
                log.Info("Info");
            }

            System.IO.File.Exists(logName).Should().BeTrue();

            var logText = System.IO.File.ReadAllText(logName);
            logText.Should().ContainAll("Fatal", "Error");
            logText.Should().NotContain("Info");
        }
        
        [Test]
        public void Should_append_to_file_by_default()
        {
            var logName = Folder.GetFileName("log");

            using (var log = new FileLog(new FileLogSettings {FilePath = logName}))
            {
                log.Info(FormatMessage(0));
            }
            
            using (var log = new FileLog(new FileLogSettings { FilePath = logName }))
            {
                log.Info(FormatMessage(1));
            }

            System.IO.File.Exists(logName).Should().BeTrue();
            ShouldContainMessages(logName, new []{ FormatMessage(0), FormatMessage(1) });
        }
        
        [Test]
        public void Should_rewrite_file_when_configured()
        {
            var logName = Folder.GetFileName("log");

            using (var log = new FileLog(new FileLogSettings {FilePath = logName}))
            {
                log.Info(FormatMessage(0));
            }

            System.IO.File.Exists(logName).Should().BeTrue();
            ShouldContainMessage(logName, FormatMessage(0));
            
            using (var log = new FileLog(new FileLogSettings { FilePath = logName, FileOpenMode = FileOpenMode.Rewrite }))
            {
                log.Info(FormatMessage(1));
            }

            var logText = System.IO.File.ReadAllText(logName);
            logText.Should().NotContain(FormatMessage(0));
            logText.Should().Contain(FormatMessage(1));
        }
        
        [Test]
        public void Should_create_directories_leading_to_log_file_path_if_needed()
        {
            var logName = Path.Combine(Folder.Name, "dir1", "dir2", "log");

            var messages = GenerateMessages(0, 3);
            
            using (var log = new FileLog(new FileLogSettings { FilePath = logName }))
            {
                WriteMessagesWithTimeout(log, messages, 0);
            }

            System.IO.File.Exists(logName).Should().BeTrue();

            ShouldContainMessages(logName, messages);
        }
    }
}