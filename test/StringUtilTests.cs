using FluentAssertions;
using Soenneker.Tests.Unit;
using Xunit;
using Xunit.Abstractions;

namespace Soenneker.Utils.String.Tests;

public class StringUtilTests : UnitTest
{
    public StringUtilTests(ITestOutputHelper output) : base(output)
    {
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
}