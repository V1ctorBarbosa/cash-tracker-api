using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
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

    [Fact]
    public void ForUncategorized_ReturnsTheSameId_ForTheSameUserAndKind()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");

        // Act
        var first = TagId.ForUncategorized(userId, TransactionType.Expense);
        var second = TagId.ForUncategorized(userId, TransactionType.Expense);

        // Assert
        first.Should().Be(second);
    }

    [Fact]
    public void ForUncategorized_ReturnsDifferentIds_ForDifferentKinds()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");

        // Act
        var expenseId = TagId.ForUncategorized(userId, TransactionType.Expense);
        var incomeId = TagId.ForUncategorized(userId, TransactionType.Income);

        // Assert
        expenseId.Should().NotBe(incomeId);
    }

    [Fact]
    public void ForUncategorized_ReturnsDifferentIds_ForDifferentUsers()
    {
        // Act
        var firstUserId = TagId.ForUncategorized(new UserId("firebase-uid-123"), TransactionType.Expense);
        var secondUserId = TagId.ForUncategorized(new UserId("firebase-uid-456"), TransactionType.Expense);

        // Assert
        firstUserId.Should().NotBe(secondUserId);
    }
}
