using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Tests.Functional
{
    internal class FileLogFunctionalTestsBase
    {
        protected TemporaryFolder Folder;
        
        [SetUp]
        public void SetUp()
        {
            Folder = new TemporaryFolder();
        }

        [TearDown]
        public void TearDown()
        {
            Folder.Dispose();
        }
        
        protected static string FormatMessage(int index) =>
            $"Message #{index}";

        protected static string[] GenerateMessages(int startIndex, int endIndex)
        {
            return Enumerable
                .Range(startIndex, endIndex - startIndex)
                .Select(FormatMessage)
                .ToArray();
        }

        protected static void WriteMessagesWithTimeout(FileLog log, IEnumerable<string> messages, int sleepTimeout = 10)
        {
            foreach (var message in messages)
            {
                log.Info(message);
                Thread.Sleep(sleepTimeout);
            }
        }

        protected static void ShouldContainMessages(FilePath fileName, IEnumerable<string> messages)
        {
            System.IO.File.ReadAllText(fileName.NormalizedPath).Should().ContainAll(messages);
        }
        
        protected static void ShouldContainMessage(FilePath fileName, string message)
        {
            ShouldContainMessages(fileName, new []{message});
        }
    }
}