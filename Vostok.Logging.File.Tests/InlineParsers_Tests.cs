using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Logging.File.Tests
{
    [TestFixture]
    internal class InlineParsers_Tests
    {
        [TestCase(typeof(Encoding))]
        public void TryParse_should_return_correct_results_for_supported_InlineParsers(Type parserType)
        {
            var inlineParser = typeToParserTest[parserType].parser;
            var scenarios = typeToParserTest[parserType].scenarios;

            foreach (var (input, boolResult, valueResult) in scenarios)
            {
                inlineParser.TryParse(input, out var result).Should().Be(boolResult);
                result.Should().Be(valueResult);
            }
        }

        private readonly Dictionary<Type, (IInlineParser parser, (string input, bool boolResult, object valueResult)[] scenarios)>
            typeToParserTest = new Dictionary<Type, (IInlineParser, (string, bool, object)[])>
            {
                {typeof(Encoding), (new InlineParser<Encoding>(EncodingParser.TryParse), new (string, bool, object)[]
                {
                    (null, false, null),
                    (string.Empty, false, null),
                    ("Hello, World", false, null),
                    ("utf-8", true, Encoding.UTF8)
                })}
            };
    }
}