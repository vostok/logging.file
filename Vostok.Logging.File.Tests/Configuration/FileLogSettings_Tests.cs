using System;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Tests.Configuration
{
    [TestFixture]
    internal class FileLogSettings_Tests
    {
        [Test]
        public void Clone_should_copy_all_properties()
        {
            var obj = new FileLogSettings();
            var clone = obj.Clone();
            foreach (var property in obj.GetType().GetProperties())
                property.SetValue(obj, GetDefault(property.PropertyType));

            clone.Should().BeEquivalentTo(new FileLogSettings());
        }

        private static object GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}