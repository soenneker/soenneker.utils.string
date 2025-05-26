using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Soenneker.Tests.FixturedUnit;
using Soenneker.Utils.String.Abstract;
using Soenneker.Utils.String.Tests.Dtos;
using Xunit;


namespace Soenneker.Utils.String.Tests;

[Collection("Collection")]
public class StringUtilTests : FixturedUnitTest
{
    private readonly IStringUtil _util;

    public StringUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IStringUtil>(true);
    }

    [Fact]
    public void ToCombinedId_with_null_should_return_expected()
    {
        string result = StringUtil.ToCombinedId();

        result.Should().Be("");
    }

    [Fact]
    public void ToCombinedId_with_double_null_should_return_expected()
    {
        string result = StringUtil.ToCombinedId(null, null);

        result.Should().Be("");
    }

    [Fact]
    public void ToCombinedId_with_value_and_null_should_return_expected()
    {
        string result = StringUtil.ToCombinedId("test", null);

        result.Should().Be("test");
    }

    [Fact]
    public void ToCombinedId_with_values_should_return_expected()
    {
        string result = StringUtil.ToCombinedId("test", "two");

        result.Should().Be("test:two");
    }

    [Fact]
    public void ToCombinedId_with_empty_should_return_expected()
    {
        string result = StringUtil.ToCombinedId("test", "");

        result.Should().Be("test");
    }

    [Fact]
    public void ToCombinedId_with_whitespace_should_return_expected()
    {
        string result = StringUtil.ToCombinedId(" ", "test");

        result.Should().Be(" :test");
    }

    [Fact]
    public void GetQueryParameter_should_get_value()
    {
        string? result = StringUtil.GetQueryParameter("https://example.com/page?param1=value1&param2=value2&param3=value3", "param1");

        result.Should().Be("value1");
    }

    [Fact]
    public void GetQueryParameter_with_no_parameter_should_return_null()
    {
        string? result = StringUtil.GetQueryParameter("https://example.com/page", "param1");

        result.Should().BeNull();
    }

    [Fact]
    public void BuildStringFromTemplate_with_one_param()
    {
        string result = StringUtil.BuildStringFromTemplate("{test} blah", 3);

        result.Should().Be("3 blah");
    }

    [Fact]
    public void BuildStringFromTemplate_with_two_params()
    {
        string result = StringUtil.BuildStringFromTemplate("{test} blah {test}", 3, 4);

        result.Should().Be("3 blah 4");
    }

    [Fact]
    public void BuildStringFromTemplate_with_two_params_not_fully_braced()
    {
        string result = StringUtil.BuildStringFromTemplate("{test} blah {test", 3, 4);

        result.Should().Be("3 blah {test");
    }

    [Fact]
    public void BuildStringFromTemplate_with_null_param()
    {
        string result = StringUtil.BuildStringFromTemplate("{test} blah {bar}", 3, null, 5);

        result.Should().Be("3 blah 5");
    }

    [Fact]
    public void BuildStringFromTemplate_with_only_null_param()
    {
        string result = StringUtil.BuildStringFromTemplate("{test} blah {bar}", null);

        result.Should().Be("{test} blah {bar}");
    }

    [Fact]
    public void BuildStringFromTemplate_with_no_param()
    {
        string result = StringUtil.BuildStringFromTemplate("{test} blah {bar}");

        result.Should().Be("{test} blah {bar}");
    }

    [Fact]
    public void GetDomainFromEmail_should_get_domain()
    {
        const string test = "blah@blah.com";
        string? result = _util.GetDomainFromEmail(test);
        result.Should().Be("blah.com");
    }

    [Theory]
    [InlineData("blahhttps://google.com", "https://google.com")]
    [InlineData("blah https://google.com", "https://google.com")]
    [InlineData("[url=https://google.com]", "https://google.com")]
    [InlineData("[url=http://google.com]", "http://google.com")]
    [InlineData("foowww.google.com]", "www.google.com")]
    [InlineData("google.com", null)]
    public void ExtractUrls_should_extract(string input, string? expected)
    {
        string? result = StringUtil.ExtractUrls(input)?.FirstOrDefault();
        result.Should().Be(expected);
    }

    [Fact]
    public void ExtractUrls_should_extract_multiple()
    {
        List<string>? result = StringUtil.ExtractUrls("https://google.com https://www.foobar.com blue")!;
        result.Count.Should().Be(2);
    }

    [Fact]
    public void ParseQueryString_ShouldParseStringToModel()
    {
        // Arrange
        var queryString = "Param1=value1&Param2=123&Param3=true";

        // Act
        QueryDto result = _util.ParseQueryString<QueryDto>(queryString);

        // Assert
        result.Param1.Should().Be("value1");
        result.Param2.Should().Be(123);
        result.Param3.Should().BeTrue();
    }

    [Fact]
    public void ParseQueryString_ShouldHandleMissingParameters()
    {
        // Arrange
        var queryString = "Param1=value1";

        // Act
        QueryDto result = _util.ParseQueryString<QueryDto>(queryString);

        // Assert
        result.Param1.Should().Be("value1");
        result.Param2.Should().Be(0); // default int value
        result.Param3.Should().BeFalse(); // default bool value
    }

    [Fact]
    public void ParseQueryString_with_question_ShouldHandleMissingParameters()
    {
        // Arrange
        var queryString = "?Param1=value1";

        // Act
        QueryDto result = _util.ParseQueryString<QueryDto>(queryString);

        // Assert
        result.Param1.Should().Be("value1");
        result.Param2.Should().Be(0); // default int value
        result.Param3.Should().BeFalse(); // default bool value
    }

    [Fact]
    public void ParseQueryString_ShouldHandleInvalidConversions()
    {
        // Arrange
        string queryString = "Param2=invalid";

        // Act
        Action act = () => _ = _util.ParseQueryString<QueryDto>(queryString);

        // Assert
        act.Should().Throw<FormatException>();
    }
}