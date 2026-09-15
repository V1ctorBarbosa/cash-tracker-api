using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
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

    [Fact]
    public void ForRecurring_IsDeterministic_ForTheSameRecurringTransactionAndMonth()
    {
        // Arrange
        var recurringTransactionId = new RecurringTransactionId("recurring-1");
        var month = new YearMonth(2026, 9);

        // Act
        var first = TransactionId.ForRecurring(recurringTransactionId, month);
        var second = TransactionId.ForRecurring(recurringTransactionId, month);

        // Assert
        first.Should().Be(second);
    }

    [Fact]
    public void ForRecurring_ProducesDifferentIds_ForDifferentMonths()
    {
        // Arrange
        var recurringTransactionId = new RecurringTransactionId("recurring-1");

        // Act
        var september = TransactionId.ForRecurring(recurringTransactionId, new YearMonth(2026, 9));
        var october = TransactionId.ForRecurring(recurringTransactionId, new YearMonth(2026, 10));

        // Assert
        september.Should().NotBe(october);
    }

    [Fact]
    public void ForRecurring_ProducesDifferentIds_ForDifferentRecurringTransactions()
    {
        // Arrange
        var month = new YearMonth(2026, 9);

        // Act
        var first = TransactionId.ForRecurring(new RecurringTransactionId("recurring-1"), month);
        var second = TransactionId.ForRecurring(new RecurringTransactionId("recurring-2"), month);

        // Assert
        first.Should().NotBe(second);
    }
}
