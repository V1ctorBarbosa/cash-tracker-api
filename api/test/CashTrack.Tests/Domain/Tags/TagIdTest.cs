using CashTrack.Domain.Tags;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Tags;

public sealed class TagIdTest
{
    [Fact]
    public void Constructor_SetsValue_WhenGivenNonEmptyString()
    {
        // Act
        var tagId = new TagId("tag-mercado");

        // Assert
        tagId.Value.Should().Be("tag-mercado");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsEmptyOrWhitespace(string value)
    {
        // Act
        var act = () => new TagId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
