using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Tests.Configuration
{
    [TestFixture]
    internal class SettingsValidator_Tests
    {
        [Test]
        public void ValidateSettings_should_allow_default_settings()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings()))
                .Should().NotThrow();
        }

        [Test]
        public void ValidateSettings_should_not_allow_null_or_empty_FilePath()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { FilePath = @"" }))
                .Should().Throw<ArgumentNullException>();

            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { FilePath = null }))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ValidateSettings_should_not_allow_FilePath_pointing_to_a_directory()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs" + Path.DirectorySeparatorChar) }))
                .Should().Throw<ArgumentException>();
        }

        [Test]
        public void ValidateSettings_should_not_allow_null_OutputTemplate()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { OutputTemplate = null }))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ValidateSettings_should_not_allow_invalid_FileOpenMode()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { FileOpenMode = (FileOpenMode)(-1) }))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ValidateSettings_should_not_allow_null_RollingStrategy()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { RollingStrategy = null }))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ValidateSettings_should_allow_zero_RollingStrategy_MaxFiles([Values] RollingStrategyType type)
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings {RollingStrategy = new RollingStrategyOptions {MaxFiles = 0}}))
                .Should().NotThrow();
        }

        [TestCase(0, RollingStrategyType.BySize)]
        [TestCase(-1, RollingStrategyType.BySize)]
        [TestCase(0, RollingStrategyType.Hybrid)]
        [TestCase(-1, RollingStrategyType.Hybrid)]
        public void ValidateSettings_should_not_allow_non_positive_RollingStrategy_MaxSize_for_BySize_and_Hybrid_strategies(int maxSize, RollingStrategyType type)
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { RollingStrategy = new RollingStrategyOptions { MaxSize = maxSize, Type = type } }))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase(RollingStrategyType.None)]
        [TestCase(RollingStrategyType.ByTime)]
        public void ValidateSettings_should_allow_zero_RollingStrategy_MaxSize_for_None_and_ByTime_strategies(RollingStrategyType type)
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { RollingStrategy = new RollingStrategyOptions { MaxSize = 0, Type = type } }))
                .Should().NotThrow();
        }

        [TestCase(RollingStrategyType.Hybrid)]
        [TestCase(RollingStrategyType.ByTime)]
        public void ValidateSettings_should_not_allow_invalid_RollingStrategy_Period(RollingStrategyType type)
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { RollingStrategy = new RollingStrategyOptions { Period = (RollingPeriod)(-1), Type = type } }))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ValidateSettings_should_not_allow_null_Encoding()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { Encoding = null }))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ValidateSettings_should_not_allow_non_positive_OutputBufferSize()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { OutputBufferSize = -1 }))
                .Should().Throw<ArgumentOutOfRangeException>();

            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { OutputBufferSize = 0 }))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ValidateSettings_should_not_allow_null_EnabledLogLevels()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { EnabledLogLevels = null }))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ValidateSettings_should_not_allow_non_positive_EventsQueueCapacity()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { EventsQueueCapacity = -1 }))
                .Should().Throw<ArgumentOutOfRangeException>();

            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { EventsQueueCapacity = 0 }))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ValidateSettings_should_not_allow_non_positive_EventsBufferCapacity()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { EventsBufferCapacity = -1 }))
                .Should().Throw<ArgumentOutOfRangeException>();

            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { EventsBufferCapacity = 0 }))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ValidateSettings_should_not_allow_non_positive_TargetFileUpdateCooldown()
        {
            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { FileSettingsUpdateCooldown = TimeSpan.FromSeconds(-1) }))
                .Should().Throw<ArgumentOutOfRangeException>();

            new Action(() => SettingsValidator.ValidateSettings(new FileLogSettings { FileSettingsUpdateCooldown = TimeSpan.Zero }))
                .Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}