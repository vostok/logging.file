﻿using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Commons.Testing;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.Formatting;

// ReSharper disable AccessToModifiedClosure

namespace Vostok.Logging.File.Tests.Functional
{
    [TestFixture]
    internal class FileLog_CacheAndFlush_Tests : FileLogFunctionalTestsBase
    {
        [OneTimeSetUp]
        public async Task Cleanup()
        {
            for (var i = 0; i < 2; i++)
            {
                GC.Collect();
                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Should_write_to_new_file_after_settings_update(bool cache)
        {
            var firstLog = Folder.GetFileName("firstLog");
            var secondLog = Folder.GetFileName("secondLog");

            var firstLogMessages = GenerateMessages(0, 3);
            var secondLogMessages = GenerateMessages(3, 6);

            var currentSettings = new FileLogSettings {FilePath = firstLog, EnableFileLogSettingsCache = cache};

            using (var log = new FileLog(() => currentSettings))
            {
                WriteMessagesWithTimeout(log, firstLogMessages, 0);

                currentSettings = new FileLogSettings {FilePath = secondLog};
                FileLog.RefreshAllSettings();

                WriteMessagesWithTimeout(log, secondLogMessages, 0);
            }

            ShouldContainMessages(firstLog, firstLogMessages);
            ShouldContainMessages(secondLog, secondLogMessages);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Should_respect_writer_setting_updates(bool cache)
        {
            var logFile = Folder.GetFileName("firstLog");

            var firstMessages = GenerateMessages(0, 3);
            var secondMessages = GenerateMessages(3, 6);

            var currentSettings = new FileLogSettings {FilePath = logFile, EnableFileLogSettingsCache = cache};

            using (var log = new FileLog(() => currentSettings))
            {
                WriteMessagesWithTimeout(log, firstMessages, 0);

                log.Flush();

                currentSettings = new FileLogSettings {FilePath = logFile, FileOpenMode = FileOpenMode.Rewrite, EnableFileLogSettingsCache = cache};
                FileLog.RefreshAllSettings();

                WriteMessagesWithTimeout(log, secondMessages, 0);
            }

            ShouldContainMessages(logFile, secondMessages);
            ShouldNotContainMessages(logFile, firstMessages);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Should_respect_writer_format_setting_updates(bool cache)
        {
            var logFile = Folder.GetFileName("firstLog");

            var firstMessages = GenerateMessages(0, 3);
            var secondMessages = GenerateMessages(3, 6);

            var currentSettings = new FileLogSettings {FilePath = logFile, OutputTemplate = OutputTemplate.Default, EnableFileLogSettingsCache = cache};

            using (var log = new FileLog(() => currentSettings))
            {
                WriteMessagesWithTimeout(log, firstMessages, 0);

                log.Flush();

                currentSettings = new FileLogSettings {FilePath = logFile, OutputTemplate = OutputTemplate.Empty, EnableFileLogSettingsCache = cache};
                FileLog.RefreshAllSettings();

                WriteMessagesWithTimeout(log, secondMessages, 0);
            }

            ShouldContainMessages(logFile, firstMessages);
            ShouldNotContainMessages(logFile, secondMessages);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Should_not_reopen_files_if_log_was_disposed(bool cache)
        {
            var logFile = Folder.GetFileName("firstLog");

            var messages = GenerateMessages(0, 3);
            var log = new FileLog(new FileLogSettings {FilePath = logFile, FileShare = FileShare.None, EnableFileLogSettingsCache = cache});

            // Let's open file exclusively to ensure that no one else uses it.
            using (log)
            {
                WriteMessagesWithTimeout(log, messages, 0);
            }

            Action assertion = () => System.IO.File.Open(logFile, FileMode.Append, FileAccess.Write, FileShare.None).Close();

            assertion.Should().NotThrow();

            FileLog.RefreshAllSettings();

            assertion.ShouldPassIn(5.Seconds());
        }
    }
}