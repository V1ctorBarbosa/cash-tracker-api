using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Budgets;

public sealed class BudgetIdTest
{
    [Fact]
    public void Constructor_SetsValue_WhenGivenNonEmptyString()
    {
        // Act
        var id = new BudgetId("budget-1");

        // Assert
        id.Value.Should().Be("budget-1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsEmptyOrWhitespace(string value)
    {
        // Act
        var act = () => new BudgetId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void For_IsDeterministic_ForTheSameUserTagAndMonth()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var tagId = new TagId("tag-lazer");
        var month = new YearMonth(2026, 9);

        // Act
        var first = BudgetId.For(userId, tagId, month);
        var second = BudgetId.For(userId, tagId, month);

        // Assert
        first.Should().Be(second);
    }

    [Fact]
    public void For_ProducesDifferentIds_ForDifferentMonths()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var tagId = new TagId("tag-lazer");

        // Act
        var september = BudgetId.For(userId, tagId, new YearMonth(2026, 9));
        var october = BudgetId.For(userId, tagId, new YearMonth(2026, 10));

        // Assert
        september.Should().NotBe(october);
    }
}
