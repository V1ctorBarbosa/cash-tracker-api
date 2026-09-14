using CashTrack.Domain.Users;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Users;

public sealed class UserIdTest
{
    [Fact]
    public void Constructor_SetsValue_WhenGivenNonEmptyString()
    {
        // Act
        var userId = new UserId("firebase-uid-123");

        // Assert
        userId.Value.Should().Be("firebase-uid-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsEmptyOrWhitespace(string value)
    {
        // Act
        var act = () => new UserId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
