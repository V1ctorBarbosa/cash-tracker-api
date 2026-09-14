using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using CashTrack.Infrastructure.RecurringTransactions;
using FluentAssertions;

namespace CashTrack.Tests.Infrastructure.RecurringTransactions;

public sealed class RecurringTransactionDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromRecurringTransaction()
    {
        // Arrange
        var recurringTransaction = RecurringTransaction.Create(
            new RecurringTransactionId("recurring-1"),
            new UserId("firebase-uid-123"),
            TransactionType.Expense,
            new Money(1800.00m),
            "Aluguel",
            new TagId("tag-moradia"),
            PaymentMethod.TransferenciaBancaria,
            5,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            true);

        // Act
        var document = RecurringTransactionDocumentMapper.ToDocument(recurringTransaction);

        // Assert
        document.UserId.Should().Be("firebase-uid-123");
        document.Type.Should().Be(nameof(TransactionType.Expense));
        document.Amount.Should().Be(1800.00m);
        document.Description.Should().Be("Aluguel");
        document.TagId.Should().Be("tag-moradia");
        document.PaymentMethod.Should().Be(nameof(PaymentMethod.TransferenciaBancaria));
        document.DayOfMonth.Should().Be(5);
        document.StartDate.Should().Be("2026-01-01");
        document.EndDate.Should().Be("2026-12-31");
        document.Active.Should().BeTrue();
    }

    [Fact]
    public void ToDocument_MapsNullEndDate_AsNull()
    {
        // Arrange
        var recurringTransaction = RecurringTransaction.Create(
            new RecurringTransactionId("recurring-1"),
            new UserId("firebase-uid-123"),
            TransactionType.Expense,
            new Money(1800.00m),
            "Aluguel",
            new TagId("tag-moradia"),
            PaymentMethod.TransferenciaBancaria,
            5,
            new DateOnly(2026, 1, 1),
            null,
            true);

        // Act
        var document = RecurringTransactionDocumentMapper.ToDocument(recurringTransaction);

        // Assert
        document.EndDate.Should().BeNull();
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument()
    {
        // Arrange
        var id = new RecurringTransactionId("recurring-1");
        var originalRecurringTransaction = RecurringTransaction.Create(
            id,
            new UserId("firebase-uid-123"),
            TransactionType.Income,
            new Money(5200.00m),
            "Salário",
            new TagId("tag-salario"),
            PaymentMethod.Pix,
            1,
            new DateOnly(2026, 1, 1),
            null,
            false);

        // Act
        var document = RecurringTransactionDocumentMapper.ToDocument(originalRecurringTransaction);
        var roundTripped = RecurringTransactionDocumentMapper.ToDomain(id, document);

        // Assert
        roundTripped.Id.Should().Be(id);
        roundTripped.UserId.Should().Be(originalRecurringTransaction.UserId);
        roundTripped.Type.Should().Be(originalRecurringTransaction.Type);
        roundTripped.Amount.Should().Be(originalRecurringTransaction.Amount);
        roundTripped.Description.Should().Be(originalRecurringTransaction.Description);
        roundTripped.TagId.Should().Be(originalRecurringTransaction.TagId);
        roundTripped.PaymentMethod.Should().Be(originalRecurringTransaction.PaymentMethod);
        roundTripped.DayOfMonth.Should().Be(originalRecurringTransaction.DayOfMonth);
        roundTripped.StartDate.Should().Be(originalRecurringTransaction.StartDate);
        roundTripped.EndDate.Should().Be(originalRecurringTransaction.EndDate);
        roundTripped.Active.Should().Be(originalRecurringTransaction.Active);
    }
}
