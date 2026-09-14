using CashTrack.Application.Transactions;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace CashTrack.Tests.Application.Transactions;

public sealed class TransactionServiceTest
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly TransactionService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public TransactionServiceTest()
    {
        _sut = new TransactionService(_transactionRepository);
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedDtos_AndBuildsQuery_FromFilters()
    {
        // Arrange
        var transaction = CreateValidTransaction();
        _transactionRepository
            .ListByUserAsync(_userId, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns([transaction]);

        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 30);

        // Act
        var result = await _sut.ListAsync(_userId, from, to, "tag-mercado", "Expense", "CartaoDebito");

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be("transaction-1");
        await _transactionRepository.Received(1).ListByUserAsync(
            _userId,
            Arg.Is<TransactionQuery>(query =>
                query.From == from
                && query.To == to
                && query.TagId == new TagId("tag-mercado")
                && query.Type == TransactionType.Expense
                && query.PaymentMethod == PaymentMethod.CartaoDebito),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_BuildsQuery_WithNullFilters_WhenNoneProvided()
    {
        // Arrange
        _transactionRepository
            .ListByUserAsync(_userId, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        await _sut.ListAsync(_userId, from: null, to: null, tagId: null, type: null, paymentMethod: null);

        // Assert
        await _transactionRepository.Received(1).ListByUserAsync(
            _userId,
            Arg.Is<TransactionQuery>(query =>
                query.From == null && query.To == null && query.TagId == null && query.Type == null && query.PaymentMethod == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenTransactionDoesNotExist()
    {
        // Arrange
        var transactionId = new TransactionId("transaction-1");
        _transactionRepository.GetByIdAsync(_userId, transactionId, Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        // Act
        var result = await _sut.GetAsync(_userId, transactionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_SavesTransaction_OwnedByRequestingUser()
    {
        // Arrange
        var request = new CreateTransactionRequest("Expense", 89.90m, new DateOnly(2026, 9, 10), "Supermercado", "tag-mercado", "CartaoDebito");

        // Act
        var result = await _sut.CreateAsync(_userId, request);

        // Assert
        result.Type.Should().Be("Expense");
        result.Amount.Should().Be(89.90m);
        result.Description.Should().Be("Supermercado");
        await _transactionRepository.Received(1).SaveAsync(
            Arg.Is<Transaction>(transaction => transaction.UserId == _userId && transaction.Description == "Supermercado"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenTransactionDoesNotBelongToUser()
    {
        // Arrange
        var transactionId = new TransactionId("transaction-1");
        _transactionRepository.GetByIdAsync(_userId, transactionId, Arg.Any<CancellationToken>()).Returns((Transaction?)null);
        var request = new UpdateTransactionRequest("Expense", 89.90m, new DateOnly(2026, 9, 10), "Supermercado", "tag-mercado", "CartaoDebito");

        // Act
        var result = await _sut.UpdateAsync(_userId, transactionId, request);

        // Assert
        result.Should().BeNull();
        await _transactionRepository.DidNotReceive().SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_SavesUpdatedTransaction_PreservingCreatedAt_WhenTransactionBelongsToUser()
    {
        // Arrange
        var transactionId = new TransactionId("transaction-1");
        var existingTransaction = CreateValidTransaction();
        _transactionRepository.GetByIdAsync(_userId, transactionId, Arg.Any<CancellationToken>()).Returns(existingTransaction);
        var request = new UpdateTransactionRequest("Income", 5200.00m, new DateOnly(2026, 9, 1), "Salário", "tag-salario", "Pix");

        // Act
        var result = await _sut.UpdateAsync(_userId, transactionId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be("Income");
        result.Amount.Should().Be(5200.00m);
        result.Description.Should().Be("Salário");
        await _transactionRepository.Received(1).SaveAsync(
            Arg.Is<Transaction>(transaction => transaction.CreatedAt == existingTransaction.CreatedAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenTransactionDoesNotBelongToUser()
    {
        // Arrange
        var transactionId = new TransactionId("transaction-1");
        _transactionRepository.GetByIdAsync(_userId, transactionId, Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        // Act
        var result = await _sut.DeleteAsync(_userId, transactionId);

        // Assert
        result.Should().BeFalse();
        await _transactionRepository.DidNotReceive().DeleteAsync(Arg.Any<UserId>(), Arg.Any<TransactionId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndDeletes_WhenTransactionBelongsToUser()
    {
        // Arrange
        var transactionId = new TransactionId("transaction-1");
        var existingTransaction = CreateValidTransaction();
        _transactionRepository.GetByIdAsync(_userId, transactionId, Arg.Any<CancellationToken>()).Returns(existingTransaction);

        // Act
        var result = await _sut.DeleteAsync(_userId, transactionId);

        // Assert
        result.Should().BeTrue();
        await _transactionRepository.Received(1).DeleteAsync(_userId, transactionId, Arg.Any<CancellationToken>());
    }

    private Transaction CreateValidTransaction() => Transaction.Create(
        new TransactionId("transaction-1"),
        _userId,
        TransactionType.Expense,
        new Money(89.90m),
        new DateOnly(2026, 9, 10),
        "Supermercado",
        new TagId("tag-mercado"),
        PaymentMethod.CartaoDebito,
        DateTimeOffset.UtcNow);
}
