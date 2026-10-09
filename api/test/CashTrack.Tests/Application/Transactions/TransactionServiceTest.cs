using CashTrack.Application.RecurringTransactions;
using CashTrack.Application.Tags;
using CashTrack.Application.Transactions;
using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CashTrack.Tests.Application.Transactions;

public sealed class TransactionServiceTest
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = Substitute.For<IRecurringTransactionRepository>();
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly TransactionService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public TransactionServiceTest()
    {
        // No recurring transactions by default, so materialization (triggered whenever
        // ListAsync is given a bounded range) is a real no-op instead of a mocked one --
        // RecurringTransactionMaterializationService and RecurringTransactionService are both
        // sealed and can't be substituted.
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([]);
        var materializationService = new RecurringTransactionMaterializationService(
            _recurringTransactionRepository,
            _transactionRepository,
            NullLogger<RecurringTransactionMaterializationService>.Instance);
        var recurringTransactionService = new RecurringTransactionService(_recurringTransactionRepository, _tagRepository);

        _sut = new TransactionService(_transactionRepository, _tagRepository, materializationService, recurringTransactionService);
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
    public async Task CreateAsync_Throws_WhenTagIsUncategorized_AndDescriptionIsMissing()
    {
        // Arrange
        var tagId = new TagId("tag-uncategorized");
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns(Tag.CreateUncategorized(_userId, TransactionType.Expense));
        var request = new CreateTransactionRequest("Expense", 89.90m, new DateOnly(2026, 9, 10), null, "tag-uncategorized", "CartaoDebito");

        // Act
        var act = async () => await _sut.CreateAsync(_userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _transactionRepository.DidNotReceive().SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WhenTagIsUncategorized_AndDescriptionIsProvided()
    {
        // Arrange
        var tagId = new TagId("tag-uncategorized");
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns(Tag.CreateUncategorized(_userId, TransactionType.Expense));
        var request = new CreateTransactionRequest("Expense", 89.90m, new DateOnly(2026, 9, 10), "Peça avulsa", "tag-uncategorized", "CartaoDebito");

        // Act
        var result = await _sut.CreateAsync(_userId, request);

        // Assert
        result.Description.Should().Be("Peça avulsa");
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
    public async Task UpdateAsync_Throws_WhenTagIsUncategorized_AndDescriptionIsMissing()
    {
        // Arrange
        var transactionId = new TransactionId("transaction-1");
        _transactionRepository.GetByIdAsync(_userId, transactionId, Arg.Any<CancellationToken>()).Returns(CreateValidTransaction());
        var tagId = new TagId("tag-uncategorized");
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns(Tag.CreateUncategorized(_userId, TransactionType.Expense));
        var request = new UpdateTransactionRequest("Expense", 89.90m, new DateOnly(2026, 9, 10), null, "tag-uncategorized", "CartaoDebito");

        // Act
        var act = async () => await _sut.UpdateAsync(_userId, transactionId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _transactionRepository.DidNotReceive().SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task DeleteAsync_DoesNotTouchAnyRecurringDefinition_WhenTheTransactionIsNotRecurring()
    {
        // Arrange
        var transactionId = new TransactionId("transaction-1");
        var existingTransaction = CreateValidTransaction();
        _transactionRepository.GetByIdAsync(_userId, transactionId, Arg.Any<CancellationToken>()).Returns(existingTransaction);

        // Act
        await _sut.DeleteAsync(_userId, transactionId);

        // Assert
        await _recurringTransactionRepository.DidNotReceive().SaveAsync(Arg.Any<RecurringTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_SkipsTheMonthOnTheRecurringDefinition_WhenTheTransactionWasMaterializedFromOne()
    {
        // Arrange
        var transactionId = new TransactionId("transaction-1");
        var recurringTransactionId = new RecurringTransactionId("recurring-1");
        var existingTransaction = Transaction.Create(
            transactionId,
            _userId,
            TransactionType.Income,
            new Money(5200.00m),
            new DateOnly(2026, 9, 10),
            "Salário",
            new TagId("tag-salario"),
            PaymentMethod.Pix,
            DateTimeOffset.UtcNow,
            recurringTransactionId);
        _transactionRepository.GetByIdAsync(_userId, transactionId, Arg.Any<CancellationToken>()).Returns(existingTransaction);

        var existingRecurringTransaction = RecurringTransaction.Create(
            recurringTransactionId,
            _userId,
            TransactionType.Income,
            new Money(5200.00m),
            "Salário",
            new TagId("tag-salario"),
            PaymentMethod.Pix,
            10,
            new DateOnly(2026, 1, 1),
            null,
            active: true);
        _recurringTransactionRepository
            .GetByIdAsync(_userId, recurringTransactionId, Arg.Any<CancellationToken>())
            .Returns(existingRecurringTransaction);

        // Act
        await _sut.DeleteAsync(_userId, transactionId);

        // Assert
        await _recurringTransactionRepository.Received(1).SaveAsync(
            Arg.Is<RecurringTransaction>(recurringTransaction => recurringTransaction.SkippedMonths.Contains(new YearMonth(2026, 9))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteInRangeAsync_DeletesEveryTransactionInTheRange_AndReturnsTheCount()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 30);
        var firstTransaction = CreateTransactionWithId("transaction-1");
        var secondTransaction = CreateTransactionWithId("transaction-2");
        _transactionRepository
            .ListByUserAsync(_userId, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns([firstTransaction, secondTransaction]);

        // Act
        var result = await _sut.DeleteInRangeAsync(_userId, from, to);

        // Assert
        result.DeletedCount.Should().Be(2);
        await _transactionRepository.Received(1).ListByUserAsync(
            _userId,
            Arg.Is<TransactionQuery>(query =>
                query.From == from && query.To == to && query.TagId == null && query.Type == null && query.PaymentMethod == null),
            Arg.Any<CancellationToken>());
        await _transactionRepository.Received(1).DeleteManyAsync(
            _userId,
            Arg.Is<IReadOnlyCollection<TransactionId>>(ids => ids.SequenceEqual(new[] { firstTransaction.Id, secondTransaction.Id })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteInRangeAsync_ReturnsZero_AndDeletesNothing_WhenTheRangeHasNoTransactions()
    {
        // Arrange
        _transactionRepository
            .ListByUserAsync(_userId, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _sut.DeleteInRangeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        result.DeletedCount.Should().Be(0);
        await _transactionRepository.DidNotReceive().DeleteManyAsync(
            Arg.Any<UserId>(),
            Arg.Any<IReadOnlyCollection<TransactionId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteInRangeAsync_Throws_WhenTheStartDateIsAfterTheEndDate()
    {
        // Act
        var act = () => _sut.DeleteInRangeAsync(_userId, new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 1));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _transactionRepository.DidNotReceive().DeleteManyAsync(
            Arg.Any<UserId>(),
            Arg.Any<IReadOnlyCollection<TransactionId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteInRangeAsync_SkipsTheMonthOnEveryRecurringDefinitionThatWasMaterializedIntoTheRange()
    {
        // Arrange
        var recurringTransactionId = new RecurringTransactionId("recurring-1");
        var materializedTransaction = Transaction.Create(
            new TransactionId("transaction-1"),
            _userId,
            TransactionType.Income,
            new Money(5200.00m),
            new DateOnly(2026, 9, 10),
            "Salário",
            new TagId("tag-salario"),
            PaymentMethod.Pix,
            DateTimeOffset.UtcNow,
            recurringTransactionId);
        _transactionRepository
            .ListByUserAsync(_userId, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns([materializedTransaction, CreateTransactionWithId("transaction-2")]);

        var existingRecurringTransaction = RecurringTransaction.Create(
            recurringTransactionId,
            _userId,
            TransactionType.Income,
            new Money(5200.00m),
            "Salário",
            new TagId("tag-salario"),
            PaymentMethod.Pix,
            10,
            new DateOnly(2026, 1, 1),
            null,
            active: true);
        _recurringTransactionRepository
            .GetByIdAsync(_userId, recurringTransactionId, Arg.Any<CancellationToken>())
            .Returns(existingRecurringTransaction);

        // Act
        await _sut.DeleteInRangeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await _recurringTransactionRepository.Received(1).SaveAsync(
            Arg.Is<RecurringTransaction>(recurringTransaction => recurringTransaction.SkippedMonths.Contains(new YearMonth(2026, 9))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteInRangeAsync_DoesNotTouchAnyRecurringDefinition_WhenNoneOfTheDeletedTransactionsIsRecurring()
    {
        // Arrange
        _transactionRepository
            .ListByUserAsync(_userId, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns([CreateTransactionWithId("transaction-1")]);

        // Act
        await _sut.DeleteInRangeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await _recurringTransactionRepository.DidNotReceive().SaveAsync(Arg.Any<RecurringTransaction>(), Arg.Any<CancellationToken>());
    }

    private Transaction CreateTransactionWithId(string id) => Transaction.Create(
        new TransactionId(id),
        _userId,
        TransactionType.Expense,
        new Money(89.90m),
        new DateOnly(2026, 9, 10),
        "Supermercado",
        new TagId("tag-mercado"),
        PaymentMethod.CartaoDebito,
        DateTimeOffset.UtcNow);

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
