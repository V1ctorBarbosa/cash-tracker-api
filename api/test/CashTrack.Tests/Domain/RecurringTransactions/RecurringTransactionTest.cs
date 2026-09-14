using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using FluentAssertions;

namespace CashTrack.Tests.Domain.RecurringTransactions;

public sealed class RecurringTransactionTest
{
    [Fact]
    public void Create_ReturnsRecurringTransaction_WithGivenValues()
    {
        // Act
        var recurringTransaction = CreateValidRecurringTransaction();

        // Assert
        recurringTransaction.Description.Should().Be("Aluguel");
        recurringTransaction.DayOfMonth.Should().Be(5);
        recurringTransaction.Active.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenDescriptionIsEmptyOrWhitespace(string description)
    {
        // Act
        var act = () => CreateValidRecurringTransaction(description: description);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Create_Throws_WhenDayOfMonthIsOutOfRange(int dayOfMonth)
    {
        // Act
        var act = () => CreateValidRecurringTransaction(dayOfMonth: dayOfMonth);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_Throws_WhenEndDateIsBeforeStartDate()
    {
        // Act
        var act = () => CreateValidRecurringTransaction(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2025, 12, 31));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AppliesToMonth_ReturnsFalse_WhenNotActive()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction(active: false);

        // Act
        var applies = recurringTransaction.AppliesToMonth(new YearMonth(2026, 6));

        // Assert
        applies.Should().BeFalse();
    }

    [Fact]
    public void AppliesToMonth_ReturnsFalse_WhenMonthIsBeforeStartDate()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction(startDate: new DateOnly(2026, 6, 1));

        // Act
        var applies = recurringTransaction.AppliesToMonth(new YearMonth(2026, 5));

        // Assert
        applies.Should().BeFalse();
    }

    [Fact]
    public void AppliesToMonth_ReturnsFalse_WhenMonthIsAfterEndDate()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 6, 15));

        // Act
        var applies = recurringTransaction.AppliesToMonth(new YearMonth(2026, 7));

        // Assert
        applies.Should().BeFalse();
    }

    [Fact]
    public void AppliesToMonth_ReturnsTrue_WhenMonthIsWithinRangeAndActive()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31));

        // Act
        var applies = recurringTransaction.AppliesToMonth(new YearMonth(2026, 6));

        // Assert
        applies.Should().BeTrue();
    }

    [Fact]
    public void AppliesToMonth_ReturnsTrue_WhenEndDateIsNull()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction(startDate: new DateOnly(2026, 1, 1), endDate: null);

        // Act
        var applies = recurringTransaction.AppliesToMonth(new YearMonth(2030, 1));

        // Assert
        applies.Should().BeTrue();
    }

    [Fact]
    public void GetDateForMonth_UsesDayOfMonth_WhenItFitsInTheMonth()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction(dayOfMonth: 5);

        // Act
        var date = recurringTransaction.GetDateForMonth(new YearMonth(2026, 9));

        // Assert
        date.Should().Be(new DateOnly(2026, 9, 5));
    }

    [Fact]
    public void GetDateForMonth_ClampsDayOfMonth_ToTheMonthsLastDay()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction(dayOfMonth: 31);

        // Act
        var date = recurringTransaction.GetDateForMonth(new YearMonth(2026, 2));

        // Assert
        date.Should().Be(new DateOnly(2026, 2, 28));
    }

    private static RecurringTransaction CreateValidRecurringTransaction(
        string description = "Aluguel",
        int dayOfMonth = 5,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool active = true) => RecurringTransaction.Create(
        new RecurringTransactionId("recurring-1"),
        new UserId("firebase-uid-123"),
        TransactionType.Expense,
        new Money(1800.00m),
        description,
        new TagId("tag-moradia"),
        PaymentMethod.TransferenciaBancaria,
        dayOfMonth,
        startDate ?? new DateOnly(2026, 1, 1),
        endDate,
        active);
}
