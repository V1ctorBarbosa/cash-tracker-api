using CashTrack.Domain.Tags;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Tags;

public sealed class TagColorTest
{
    [Fact]
    public void Constructor_UppercasesValue_WhenGivenValidHexColor()
    {
        // Act
        var color = new TagColor("#3b82f6");

        // Assert
        color.Value.Should().Be("#3B82F6");
    }

    [Theory]
    [InlineData("")]
    [InlineData("3B82F6")]
    [InlineData("#3B82")]
    [InlineData("#GGGGGG")]
    [InlineData("blue")]
    public void Constructor_Throws_WhenValueIsNotAValidHexColor(string value)
    {
        // Act
        var act = () => new TagColor(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
