using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Helpers;
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
        private IEventsWriterFactory eventsWriterFactory;
        private IRollingGarbageCollector garbageCollector;
        private ICooldownController cooldownController;
        private IEventsWriter eventsWriter;
        private IRollingStrategy strategy;

        [SetUp]
        public void TestSetup()
        {
            strategy = Substitute.For<IRollingStrategy>();
            strategy.GetCurrentFile(Arg.Any<FilePath>()).Returns("log");
            strategy.DiscoverExistingFiles(Arg.Any<FilePath>()).Returns(new FilePath[] {"log1", "log-2"});

            strategyProvider = Substitute.For<IRollingStrategyProvider>();
            strategyProvider.ObtainStrategy().Returns(strategy);

            eventsWriter = Substitute.For<IEventsWriter>();

            eventsWriterFactory = Substitute.For<IEventsWriterFactory>();
            eventsWriterFactory.CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>()).Returns(eventsWriter);

            garbageCollector = Substitute.For<IRollingGarbageCollector>();

            cooldownController = Substitute.For<ICooldownController>();
            cooldownController.IsCool.Returns(true);
            cooldownController.WaitForCooldownAsync().Returns(Task.CompletedTask);

            provider = new EventsWriterProvider("log", eventsWriterFactory, strategyProvider, garbageCollector, cooldownController, () => settings);

            settings = new FileLogSettings();
        }

        [Test]
        public void ObtainWriterAsync_should_return_writer_opened_by_file_system()
        {
            ObtainWriter().Should().BeSameAs(eventsWriter);
        }

        [Test]
        public void ObtainWriterAsync_should_return_same_writer_while_nothing_changes()
        {
            ObtainWriter().Should().BeSameAs(eventsWriter);
            ObtainWriter().Should().BeSameAs(eventsWriter);
            ObtainWriter().Should().BeSameAs(eventsWriter);

            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>());
        }

        [Test]
        public void ObtainWriterAsync_should_create_new_writer_if_strategy_returns_new_path()
        {
            ObtainWriter();

            strategy.GetCurrentFile(Arg.Any<FilePath>()).Returns("xxx");
            ObtainWriter();

            eventsWriterFactory.Received(1).CreateWriter("log", Arg.Any<FileLogSettings>());
            eventsWriterFactory.Received(1).CreateWriter("xxx", Arg.Any<FileLogSettings>());
        }

        [Test]
        public void ObtainWriterAsync_should_create_new_writer_if_FileOpenMode_changed()
        {
            ObtainWriter();

            settings = new FileLogSettings {FileOpenMode = FileOpenMode.Rewrite};
            ObtainWriter();

            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => s.FileOpenMode == FileOpenMode.Append));
            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => s.FileOpenMode == FileOpenMode.Rewrite));
        }

        [Test]
        public void ObtainWriterAsync_should_create_new_writer_if_Encoding_changed()
        {
            ObtainWriter();

            settings = new FileLogSettings {Encoding = Encoding.ASCII};
            ObtainWriter();

            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => Equals(s.Encoding, Encoding.UTF8)));
            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => Equals(s.Encoding, Encoding.ASCII)));
        }

        [Test]
        public void ObtainWriterAsync_should_create_new_writer_if_OutputBufferSize_changed()
        {
            ObtainWriter();

            settings = new FileLogSettings {OutputBufferSize = 42};
            ObtainWriter();

            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => s.OutputBufferSize == 65536));
            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => s.OutputBufferSize == 42));
        }

        [Test]
        public void ObtainWriterAsync_should_not_create_new_writer_while_cooldown_is_active()
        {
            ObtainWriter();

            cooldownController.IsCool.Returns(false);

            settings = new FileLogSettings {OutputBufferSize = 42};
            ObtainWriter();

            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>());
        }

        [Test]
        public void ObtainWriterAsync_should_incur_cooldown_after_failed_writer_creation()
        {
            eventsWriterFactory.CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>()).Returns(null as IEventsWriter);

            ObtainWriter();

            cooldownController.Received().IncurCooldown(settings.RollingUpdateCooldown);
        }

        [Test]
        public void ObtainWriterAsync_should_incur_cooldown_after_writer_update()
        {
            ObtainWriter();

            cooldownController.Received().IncurCooldown(settings.RollingUpdateCooldown);
        }

        [Test]
        public void ObtainWriterAsync_should_dispose_old_writer()
        {
            ObtainWriter();

            settings = new FileLogSettings {OutputBufferSize = 42};
            ObtainWriter();

            eventsWriter.Received().Dispose();
        }

        [Test]
        public void ObtainWriterAsync_should_collect_garbage()
        {
            ObtainWriter();

            settings = new FileLogSettings {OutputBufferSize = 42};
            ObtainWriter();

            garbageCollector.Received().RemoveStaleFiles(Arg.Any<FilePath[]>());
        }

        [Test]
        public void ObtainWriterAsync_should_return_immediately_if_writer_is_null_and_there_is_no_active_cooldown()
        {
            eventsWriterFactory.CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>()).Returns(null as IEventsWriter);
            ObtainWriter();

            var task = provider.ObtainWriterAsync();

            task.Wait(50);
            task.IsCompleted.Should().BeTrue();
        }

        [Test]
        public void ObtainWriterAsync_should_return_immediately_if_writer_is_not_null()
        {
            eventsWriterFactory.CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>()).Returns(eventsWriter);
            ObtainWriter();
            cooldownController.WaitForCooldownAsync().Returns(Task.Delay(1000));

            var task = provider.ObtainWriterAsync();

            task.Wait(50);
            task.IsCompleted.Should().BeTrue();
        }

        [Test]
        public void ObtainWriterAsync_wait_for_cooldown_if_writer_is_null_and_cooldown_is_set()
        {
            eventsWriterFactory.CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>()).Returns(null, eventsWriter);
            ObtainWriter();
            cooldownController.WaitForCooldownAsync().Returns(Task.Delay(200));

            var task = provider.ObtainWriterAsync();
            task.Wait(50);
            task.IsCompleted.Should().BeFalse();

            task.Wait(1000);
            task.IsCompleted.Should().BeTrue();
            task.Result.Should().BeSameAs(eventsWriter);
        }

        [Test]
        public void ObtainWriterAsync_should_perform_actions_in_order()
        {
            ObtainWriter();
            
            settings = new FileLogSettings {OutputBufferSize = 42};
            ObtainWriter();

            Received.InOrder(
                () =>
                {
                    strategyProvider.ObtainStrategy();
                    strategy.GetCurrentFile(Arg.Any<FilePath>());
                    eventsWriterFactory.CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>());
                    garbageCollector.RemoveStaleFiles(Arg.Any<FilePath[]>());
                    cooldownController.IncurCooldown(Arg.Any<TimeSpan>());

                    strategyProvider.ObtainStrategy();
                    strategy.GetCurrentFile(Arg.Any<FilePath>());
                    eventsWriter.Dispose();
                    eventsWriterFactory.CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>());
                    garbageCollector.RemoveStaleFiles(Arg.Any<FilePath[]>());
                    cooldownController.IncurCooldown(Arg.Any<TimeSpan>());
                });
        }

        [Test]
        public void Dispose_should_dispose_writer()
        {
            ObtainWriter();

            provider.Dispose();

            eventsWriter.Received().Dispose();
        }

        private IEventsWriter ObtainWriter()
        {
            return provider.ObtainWriterAsync().GetAwaiter().GetResult();
        }
    }
}