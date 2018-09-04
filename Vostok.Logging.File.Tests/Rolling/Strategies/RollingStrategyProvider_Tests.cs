using System;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File.Tests.Rolling.Strategies
{
    [TestFixture]
    internal class RollingStrategyProvider_Tests
    {
        private RollingStrategyProvider provider;
        private FileLogSettings settings;
        private IRollingStrategyFactory strategyFactory;

        [SetUp]
        public void TestSetup()
        {
            strategyFactory = Substitute.For<IRollingStrategyFactory>();

            settings = new FileLogSettings();

            provider = new RollingStrategyProvider(new FilePath("log"), strategyFactory, () => settings);
        }

        [Test]
        public void Should_create_strategy_with_correct_settings([Values] RollingStrategyType type)
        {
            settings.RollingStrategy.Type = type;

            provider.ObtainStrategy();

            strategyFactory.Received().CreateStrategy(new FilePath("log"), type, Arg.Any<Func<FileLogSettings>>());
        }

        [Test]
        public void Should_create_new_strategy_if_type_changes()
        {
            settings.RollingStrategy.Type = RollingStrategyType.None;
            provider.ObtainStrategy();

            settings.RollingStrategy.Type = RollingStrategyType.ByTime;
            provider.ObtainStrategy();

            strategyFactory.Received().CreateStrategy(new FilePath("log"), RollingStrategyType.None, Arg.Any<Func<FileLogSettings>>());
            strategyFactory.Received().CreateStrategy(new FilePath("log"), RollingStrategyType.ByTime, Arg.Any<Func<FileLogSettings>>());
        }

        [Test]
        public void Should_not_create_new_strategy_if_nothing_changes()
        {
            provider.ObtainStrategy();
            provider.ObtainStrategy();

            strategyFactory.Received(1).CreateStrategy(Arg.Any<FilePath>(), Arg.Any<RollingStrategyType>(), Arg.Any<Func<FileLogSettings>>());
        }

        [Test]
        public void Should_not_create_new_strategy_if_file_path_changes_in_settings()
        {
            provider.ObtainStrategy();

            settings.FilePath = "xxx";
            provider.ObtainStrategy();

            strategyFactory.Received(1).CreateStrategy(Arg.Any<FilePath>(), Arg.Any<RollingStrategyType>(), Arg.Any<Func<FileLogSettings>>());
        }
    }
}