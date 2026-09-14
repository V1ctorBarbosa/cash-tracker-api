using CashTrack.Domain.RecurringTransactions;
using FluentAssertions;

namespace CashTrack.Tests.Domain.RecurringTransactions;

public sealed class RecurringTransactionIdTest
{
    [Fact]
    public void Constructor_SetsValue_WhenGivenNonEmptyString()
    {
        // Act
        var id = new RecurringTransactionId("recurring-1");

        // Assert
        id.Value.Should().Be("recurring-1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsEmptyOrWhitespace(string value)
    {
        // Act
        var act = () => new RecurringTransactionId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
