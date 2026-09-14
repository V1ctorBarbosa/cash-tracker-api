using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Transactions;

public sealed class TransactionTest
{
    [Fact]
    public void Create_ReturnsTransaction_WithGivenValues()
    {
        // Arrange
        var id = new TransactionId("transaction-1");
        var userId = new UserId("firebase-uid-123");
        var amount = new Money(89.90m);
        var date = new DateOnly(2026, 9, 10);
        var tagId = new TagId("tag-mercado");
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var transaction = Transaction.Create(
            id,
            userId,
            TransactionType.Expense,
            amount,
            date,
            "Supermercado",
            tagId,
            PaymentMethod.CartaoDebito,
            createdAt);

        // Assert
        transaction.Id.Should().Be(id);
        transaction.UserId.Should().Be(userId);
        transaction.Type.Should().Be(TransactionType.Expense);
        transaction.Amount.Should().Be(amount);
        transaction.Date.Should().Be(date);
        transaction.Description.Should().Be("Supermercado");
        transaction.TagId.Should().Be(tagId);
        transaction.PaymentMethod.Should().Be(PaymentMethod.CartaoDebito);
        transaction.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Create_TrimsDescription()
    {
        // Act
        var transaction = CreateValidTransaction(description: "  Supermercado  ");

        // Assert
        transaction.Description.Should().Be("Supermercado");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenDescriptionIsEmptyOrWhitespace(string description)
    {
        // Act
        var act = () => CreateValidTransaction(description: description);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private static Transaction CreateValidTransaction(string description = "Supermercado") => Transaction.Create(
        new TransactionId("transaction-1"),
        new UserId("firebase-uid-123"),
        TransactionType.Expense,
        new Money(89.90m),
        new DateOnly(2026, 9, 10),
        description,
        new TagId("tag-mercado"),
        PaymentMethod.CartaoDebito,
        DateTimeOffset.UtcNow);
}
