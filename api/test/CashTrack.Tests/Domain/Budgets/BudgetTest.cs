using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Budgets;

public sealed class BudgetTest
{
    [Fact]
    public void Create_ReturnsBudget_WithGivenValues_AndDeterministicId()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var tagId = new TagId("tag-lazer");
        var month = new YearMonth(2026, 9);
        var limitAmount = new Money(500.00m);

        // Act
        var budget = Budget.Create(userId, tagId, month, limitAmount);

        // Assert
        budget.Id.Should().Be(BudgetId.For(userId, tagId, month));
        budget.UserId.Should().Be(userId);
        budget.TagId.Should().Be(tagId);
        budget.Month.Should().Be(month);
        budget.LimitAmount.Should().Be(limitAmount);
    }

    [Fact]
    public void Create_ReturnsSameId_WhenCalledAgainForTheSameTagAndMonth()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var tagId = new TagId("tag-lazer");
        var month = new YearMonth(2026, 9);

        // Act
        var first = Budget.Create(userId, tagId, month, new Money(500.00m));
        var second = Budget.Create(userId, tagId, month, new Money(650.00m));

        // Assert
        second.Id.Should().Be(first.Id);
        second.LimitAmount.Amount.Should().Be(650.00m);
    }
}
