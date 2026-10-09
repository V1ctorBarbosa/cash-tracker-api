using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
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

    [Fact]
    public void ToDocument_MapsNullDescription_ToNull()
    {
        // Arrange
        var transaction = Transaction.Create(
            new TransactionId("transaction-1"),
            new UserId("firebase-uid-123"),
            TransactionType.Expense,
            new Money(89.90m),
            new DateOnly(2026, 9, 10),
            null,
            new TagId("tag-mercado"),
            PaymentMethod.CartaoDebito,
            DateTimeOffset.Parse("2026-09-10T18:32:00Z"));

        // Act
        var document = TransactionDocumentMapper.ToDocument(transaction);

        // Assert
        document.Description.Should().BeNull();
    }

    [Fact]
    public void ToRecurringOccurrence_ReturnsTheRecurringIdAndTheMonthOfTheTransactionDate()
    {
        // Arrange -- moved by the user from the generated day 5 to day 20; still September's occurrence.
        var document = new TransactionDocument { Date = "2026-09-20", RecurringTransactionId = "recurring-1" };

        // Act
        var occurrence = TransactionDocumentMapper.ToRecurringOccurrence(document);

        // Assert
        occurrence.Should().Be(new RecurringOccurrence(new RecurringTransactionId("recurring-1"), new YearMonth(2026, 9)));
    }

    [Fact]
    public void ToRecurringOccurrence_ReturnsNull_WhenTheTransactionIsNotRecurring()
    {
        // Arrange
        var document = new TransactionDocument { Date = "2026-09-20", RecurringTransactionId = null };

        // Act
        var occurrence = TransactionDocumentMapper.ToRecurringOccurrence(document);

        // Assert
        occurrence.Should().BeNull();
    }

    [Fact]
    public void ParseDate_ReadsTheStoredIsoDate()
    {
        // Act
        var date = TransactionDocumentMapper.ParseDate("2025-11-03");

        // Assert
        date.Should().Be(new DateOnly(2025, 11, 3));
    }
}
