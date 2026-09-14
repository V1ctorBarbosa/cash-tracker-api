using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using CashTrack.Infrastructure.Transactions;
using FluentAssertions;

namespace CashTrack.Tests.Infrastructure.Transactions;

public sealed class TransactionDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromTransaction()
    {
        // Arrange
        var createdAt = DateTimeOffset.Parse("2026-09-10T18:32:00Z");
        var transaction = Transaction.Create(
            new TransactionId("transaction-1"),
            new UserId("firebase-uid-123"),
            TransactionType.Expense,
            new Money(89.90m),
            new DateOnly(2026, 9, 10),
            "Supermercado",
            new TagId("tag-mercado"),
            PaymentMethod.CartaoDebito,
            createdAt);

        // Act
        var document = TransactionDocumentMapper.ToDocument(transaction);

        // Assert
        document.UserId.Should().Be("firebase-uid-123");
        document.Type.Should().Be(nameof(TransactionType.Expense));
        document.Amount.Should().Be(89.90m);
        document.Date.Should().Be("2026-09-10");
        document.Description.Should().Be("Supermercado");
        document.TagId.Should().Be("tag-mercado");
        document.PaymentMethod.Should().Be(nameof(PaymentMethod.CartaoDebito));
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument()
    {
        // Arrange
        var id = new TransactionId("transaction-1");
        var originalTransaction = Transaction.Create(
            id,
            new UserId("firebase-uid-123"),
            TransactionType.Income,
            new Money(5200.00m),
            new DateOnly(2026, 9, 1),
            "Salário",
            new TagId("tag-salario"),
            PaymentMethod.Pix,
            DateTimeOffset.Parse("2026-09-01T09:00:00Z"));

        // Act
        var document = TransactionDocumentMapper.ToDocument(originalTransaction);
        var roundTrippedTransaction = TransactionDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedTransaction.Id.Should().Be(id);
        roundTrippedTransaction.UserId.Should().Be(originalTransaction.UserId);
        roundTrippedTransaction.Type.Should().Be(originalTransaction.Type);
        roundTrippedTransaction.Amount.Should().Be(originalTransaction.Amount);
        roundTrippedTransaction.Date.Should().Be(originalTransaction.Date);
        roundTrippedTransaction.Description.Should().Be(originalTransaction.Description);
        roundTrippedTransaction.TagId.Should().Be(originalTransaction.TagId);
        roundTrippedTransaction.PaymentMethod.Should().Be(originalTransaction.PaymentMethod);
        roundTrippedTransaction.CreatedAt.Should().Be(originalTransaction.CreatedAt);
    }
}
