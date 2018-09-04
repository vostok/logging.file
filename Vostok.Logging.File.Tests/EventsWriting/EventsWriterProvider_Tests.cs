using System;
using System.Text;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Rolling;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File.Tests.EventsWriting
{
    [TestFixture]
    internal class EventsWriterProvider_Tests
    {
        private EventsWriterProvider provider;

        private FileLogSettings settings;
        private IRollingStrategyProvider strategyProvider;
        private IFileSystem fileSystem;
        private IRollingGarbageCollector garbageCollector;
        private ICooldownController cooldownController;
        private IEventsWriter eventsWriter;
        private IRollingStrategy strategy;

        [SetUp]
        public void TestSetup()
        {
            strategy = Substitute.For<IRollingStrategy>();
            strategy.GetCurrentFile(Arg.Any<string>()).Returns("log");
            strategy.DiscoverExistingFiles(Arg.Any<string>()).Returns(new[] {"log1", "log2"});

            strategyProvider = Substitute.For<IRollingStrategyProvider>();
            strategyProvider.ObtainStrategy().Returns(strategy);

            eventsWriter = Substitute.For<IEventsWriter>();

            fileSystem = Substitute.For<IFileSystem>();
            fileSystem.OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), Arg.Any<int>()).Returns(eventsWriter);

            garbageCollector = Substitute.For<IRollingGarbageCollector>();

            cooldownController = Substitute.For<ICooldownController>();
            cooldownController.IsCool.Returns(true);

            provider = new EventsWriterProvider(new FilePath("log"), strategyProvider, fileSystem, garbageCollector, cooldownController, () => settings);

            settings = new FileLogSettings();
        }

        [Test]
        public void IsHealthy_should_return_true_initially()
        {
            provider.IsHealthy.Should().BeTrue();
        }

        [Test]
        public void IsHealthy_should_return_true_after_obtaining_healthy_writer()
        {
            provider.ObtainWriter();

            provider.IsHealthy.Should().BeTrue();
        }

        [Test]
        public void IsHealthy_should_return_false_if_file_could_not_be_opened()
        {
            fileSystem.OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), Arg.Any<int>()).Returns(null as IEventsWriter);

            provider.ObtainWriter();

            provider.IsHealthy.Should().BeFalse();
        }

        [Test]
        public void IsHealthy_should_return_true_after_successful_reopening_of_file()
        {
            fileSystem.OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), Arg.Any<int>()).Returns(null as IEventsWriter);

            provider.ObtainWriter();

            fileSystem.OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), Arg.Any<int>()).Returns(eventsWriter);

            provider.IsHealthy.Should().BeTrue();
        }

        [Test]
        public void ObtainWriter_should_return_writer_opened_by_file_system()
        {
            provider.ObtainWriter().Should().BeSameAs(eventsWriter);
        }

        [Test]
        public void ObtainWriter_should_return_same_writer_while_nothing_changes()
        {
            provider.ObtainWriter().Should().BeSameAs(eventsWriter);
            provider.ObtainWriter().Should().BeSameAs(eventsWriter);
            provider.ObtainWriter().Should().BeSameAs(eventsWriter);

            fileSystem.Received(1).OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), Arg.Any<int>());
        }

        [Test]
        public void ObtainWriter_should_create_new_writer_if_strategy_returns_new_path()
        {
            provider.ObtainWriter();

            strategy.GetCurrentFile(Arg.Any<string>()).Returns("xxx");
            provider.ObtainWriter();

            fileSystem.Received(1).OpenFile("log", Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), Arg.Any<int>());
            fileSystem.Received(1).OpenFile("xxx", Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), Arg.Any<int>());
        }

        [Test]
        public void ObtainWriter_should_create_new_writer_if_FileOpenMode_changed()
        {
            provider.ObtainWriter();

            settings.FileOpenMode = FileOpenMode.Rewrite;
            provider.ObtainWriter();

            fileSystem.Received(1).OpenFile(Arg.Any<string>(), FileOpenMode.Append, Arg.Any<Encoding>(), Arg.Any<int>());
            fileSystem.Received(1).OpenFile(Arg.Any<string>(), FileOpenMode.Rewrite, Arg.Any<Encoding>(), Arg.Any<int>());
        }

        [Test]
        public void ObtainWriter_should_create_new_writer_if_Encoding_changed()
        {
            provider.ObtainWriter();

            settings.Encoding = Encoding.ASCII;
            provider.ObtainWriter();

            fileSystem.Received(1).OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Encoding.UTF8, Arg.Any<int>());
            fileSystem.Received(1).OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Encoding.ASCII, Arg.Any<int>());
        }

        [Test]
        public void ObtainWriter_should_create_new_writer_if_OutputBufferSize_changed()
        {
            provider.ObtainWriter();

            settings.OutputBufferSize = 42;
            provider.ObtainWriter();

            fileSystem.Received(1).OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), 65536);
            fileSystem.Received(1).OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), 42);
        }

        [Test]
        public void ObtainWriter_should_not_create_new_writer_while_cooldown_is_active()
        {
            provider.ObtainWriter();

            cooldownController.IsCool.Returns(false);

            settings.OutputBufferSize = 42;
            provider.ObtainWriter();

            fileSystem.Received(1).OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), Arg.Any<int>());
        }

        [Test]
        public void ObtainWriter_should_incur_cooldown_after_failed_writer_creation()
        {
            fileSystem.OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), Arg.Any<int>()).Returns(null as IEventsWriter);

            provider.ObtainWriter();

            cooldownController.Received().IncurCooldown(settings.RollingUpdateCooldown);
        }

        [Test]
        public void ObtainWriter_should_incur_cooldown_after_writer_update()
        {
            provider.ObtainWriter();

            cooldownController.Received().IncurCooldown(settings.RollingUpdateCooldown);
        }

        [Test]
        public void ObtainWriter_should_dispose_old_writer()
        {
            provider.ObtainWriter();

            settings.OutputBufferSize = 42;
            provider.ObtainWriter();

            eventsWriter.Received().Dispose();
        }

        [Test]
        public void ObtainWriter_should_collect_garbage()
        {
            provider.ObtainWriter();

            settings.OutputBufferSize = 42;
            provider.ObtainWriter();

            garbageCollector.Received().RemoveStaleFiles(Arg.Any<string[]>());
        }

        [Test]
        public void ObtainWriter_should_perform_actions_in_order()
        {
            provider.ObtainWriter();
            
            settings.OutputBufferSize = 42;
            provider.ObtainWriter();

            Received.InOrder(
                () =>
                {
                    strategyProvider.ObtainStrategy();
                    strategy.GetCurrentFile(Arg.Any<string>());
                    fileSystem.OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), Arg.Any<int>());
                    garbageCollector.RemoveStaleFiles(Arg.Any<string[]>());
                    cooldownController.IncurCooldown(Arg.Any<TimeSpan>());

                    strategyProvider.ObtainStrategy();
                    strategy.GetCurrentFile(Arg.Any<string>());
                    eventsWriter.Dispose();
                    fileSystem.OpenFile(Arg.Any<string>(), Arg.Any<FileOpenMode>(), Arg.Any<Encoding>(), Arg.Any<int>());
                    garbageCollector.RemoveStaleFiles(Arg.Any<string[]>());
                    cooldownController.IncurCooldown(Arg.Any<TimeSpan>());
                });
        }

        [Test]
        public void ObtainWriter_should_throw_after_dispose()
        {
            provider.ObtainWriter();

            provider.Dispose();

            new Action(() => provider.ObtainWriter()).Should().Throw<ObjectDisposedException>();
        }

        [Test]
        public void Dispose_should_dispose_writer()
        {
            provider.ObtainWriter();

            provider.Dispose();

            eventsWriter.Received().Dispose();
        }
    }
}