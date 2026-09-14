using CashTrack.Domain.Transactions;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Transactions;

public sealed class TransactionIdTest
{
    [Fact]
    public void Constructor_SetsValue_WhenGivenNonEmptyString()
    {
        // Act
        var transactionId = new TransactionId("transaction-1");

        // Assert
        transactionId.Value.Should().Be("transaction-1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsEmptyOrWhitespace(string value)
    {
        // Act
        var act = () => new TransactionId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
