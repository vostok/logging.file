using System;
using System.Text;
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
    [TestFixture, Ignore("Obsolete tests")]
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

            provider = new EventsWriterProvider("log", strategyProvider, eventsWriterFactory, garbageCollector, cooldownController, () => settings);

            settings = new FileLogSettings();
        }

        [Test]
        public void ObtainWriter_should_return_writer_opened_by_file_system()
        {
            provider.ObtainWriterAsync().Should().BeSameAs(eventsWriter);
        }

        [Test]
        public void ObtainWriter_should_return_same_writer_while_nothing_changes()
        {
            provider.ObtainWriterAsync().Should().BeSameAs(eventsWriter);
            provider.ObtainWriterAsync().Should().BeSameAs(eventsWriter);
            provider.ObtainWriterAsync().Should().BeSameAs(eventsWriter);

            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>());
        }

        [Test]
        public void ObtainWriter_should_create_new_writer_if_strategy_returns_new_path()
        {
            provider.ObtainWriterAsync();

            strategy.GetCurrentFile(Arg.Any<FilePath>()).Returns("xxx");
            provider.ObtainWriterAsync();

            eventsWriterFactory.Received(1).CreateWriter("log", Arg.Any<FileLogSettings>());
            eventsWriterFactory.Received(1).CreateWriter("xxx", Arg.Any<FileLogSettings>());
        }

        [Test]
        public void ObtainWriter_should_create_new_writer_if_FileOpenMode_changed()
        {
            provider.ObtainWriterAsync();

            settings = new FileLogSettings {FileOpenMode = FileOpenMode.Rewrite};
            provider.ObtainWriterAsync();

            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => s.FileOpenMode == FileOpenMode.Append));
            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => s.FileOpenMode == FileOpenMode.Rewrite));
        }

        [Test]
        public void ObtainWriter_should_create_new_writer_if_Encoding_changed()
        {
            provider.ObtainWriterAsync();

            settings = new FileLogSettings {Encoding = Encoding.ASCII};
            provider.ObtainWriterAsync();

            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => Equals(s.Encoding, Encoding.UTF8)));
            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => Equals(s.Encoding, Encoding.ASCII)));
        }

        [Test]
        public void ObtainWriter_should_create_new_writer_if_OutputBufferSize_changed()
        {
            provider.ObtainWriterAsync();

            settings = new FileLogSettings {OutputBufferSize = 42};
            provider.ObtainWriterAsync();

            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => s.OutputBufferSize == 65536));
            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Is<FileLogSettings>(s => s.OutputBufferSize == 42));
        }

        [Test]
        public void ObtainWriter_should_not_create_new_writer_while_cooldown_is_active()
        {
            provider.ObtainWriterAsync();

            cooldownController.IsCool.Returns(false);

            settings = new FileLogSettings {OutputBufferSize = 42};
            provider.ObtainWriterAsync();

            eventsWriterFactory.Received(1).CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>());
        }

        [Test]
        public void ObtainWriter_should_incur_cooldown_after_failed_writer_creation()
        {
            eventsWriterFactory.CreateWriter(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>()).Returns(null as IEventsWriter);

            provider.ObtainWriterAsync();

            cooldownController.Received().IncurCooldown(settings.RollingUpdateCooldown);
        }

        [Test]
        public void ObtainWriter_should_incur_cooldown_after_writer_update()
        {
            provider.ObtainWriterAsync();

            cooldownController.Received().IncurCooldown(settings.RollingUpdateCooldown);
        }

        [Test]
        public void ObtainWriter_should_dispose_old_writer()
        {
            provider.ObtainWriterAsync();

            settings = new FileLogSettings {OutputBufferSize = 42};
            provider.ObtainWriterAsync();

            eventsWriter.Received().Dispose();
        }

        [Test]
        public void ObtainWriter_should_collect_garbage()
        {
            provider.ObtainWriterAsync();

            settings = new FileLogSettings {OutputBufferSize = 42};
            provider.ObtainWriterAsync();

            garbageCollector.Received().RemoveStaleFiles(Arg.Any<FilePath[]>());
        }

        [Test]
        public void ObtainWriter_should_perform_actions_in_order()
        {
            provider.ObtainWriterAsync();
            
            settings = new FileLogSettings {OutputBufferSize = 42};
            provider.ObtainWriterAsync();

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
        public void ObtainWriter_should_throw_after_dispose()
        {
            provider.ObtainWriterAsync();

            provider.Dispose();

            new Action(() => provider.ObtainWriterAsync()).Should().Throw<ObjectDisposedException>();
        }

        [Test]
        public void Dispose_should_dispose_writer()
        {
            provider.ObtainWriterAsync();

            provider.Dispose();

            eventsWriter.Received().Dispose();
        }
    }
}