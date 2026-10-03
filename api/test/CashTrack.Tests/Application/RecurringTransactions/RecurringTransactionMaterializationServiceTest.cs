using CashTrack.Application.RecurringTransactions;
using CashTrack.Application.Transactions;
using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Transaction = CashTrack.Domain.Transactions.Transaction;

namespace CashTrack.Tests.Application.RecurringTransactions;

public sealed class RecurringTransactionMaterializationServiceTest
{
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = Substitute.For<IRecurringTransactionRepository>();
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly RecurringTransactionMaterializationService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public RecurringTransactionMaterializationServiceTest()
    {
        _sut = new RecurringTransactionMaterializationService(
            _recurringTransactionRepository,
            _transactionRepository,
            NullLogger<RecurringTransactionMaterializationService>.Instance);
    }

    [Fact]
    public async Task MaterializeAsync_DoesNothing_WhenUserHasNoRecurringTransactions()
    {
        // Arrange
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await _transactionRepository.DidNotReceive().SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MaterializeAsync_DoesNothing_WhenRecurringTransactionDoesNotApplyToAnyMonthInRange()
    {
        // Arrange -- inactive, so AppliesToMonth is always false
        var recurringTransaction = CreateValidRecurringTransaction(active: false);
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([recurringTransaction]);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await _transactionRepository.DidNotReceive().SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MaterializeAsync_DoesNothing_WhenATransactionAlreadyExistsForThatMonth()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction();
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([recurringTransaction]);
        _transactionRepository
            .ExistsForRecurringTransactionAsync(_userId, recurringTransaction.Id, new YearMonth(2026, 9), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await _transactionRepository.DidNotReceive().SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MaterializeAsync_SavesTransaction_LinkedToTheRecurringDefinition_WhenNoneExistsYet()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction(dayOfMonth: 5);
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([recurringTransaction]);
        _transactionRepository
            .ExistsForRecurringTransactionAsync(_userId, recurringTransaction.Id, new YearMonth(2026, 9), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await _transactionRepository.Received(1).SaveAsync(
            Arg.Is<Transaction>(transaction =>
                transaction.UserId == _userId
                && transaction.RecurringTransactionId == recurringTransaction.Id
                && transaction.Date == new DateOnly(2026, 9, 5)
                && transaction.Description == recurringTransaction.Description
                && transaction.Amount == recurringTransaction.Amount),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MaterializeAsync_ClampsTheGeneratedDate_ToTheMonthsLastDay()
    {
        // Arrange -- day 31 in February
        var recurringTransaction = CreateValidRecurringTransaction(dayOfMonth: 31, startDate: new DateOnly(2026, 1, 1));
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([recurringTransaction]);
        _transactionRepository
            .ExistsForRecurringTransactionAsync(_userId, recurringTransaction.Id, new YearMonth(2026, 2), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));

        // Assert
        await _transactionRepository.Received(1).SaveAsync(
            Arg.Is<Transaction>(transaction => transaction.Date == new DateOnly(2026, 2, 28)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MaterializeAsync_UsesADeterministicTransactionId_DerivedFromTheRecurringTransactionAndMonth()
    {
        // Arrange -- a random id would let two concurrent materialization calls (e.g. the
        // dashboard's month and trailing-range queries firing together) each write their own
        // transaction for the same (recurring, month) pair, double-counting the amount.
        var recurringTransaction = CreateValidRecurringTransaction(dayOfMonth: 5);
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([recurringTransaction]);
        _transactionRepository
            .ExistsForRecurringTransactionAsync(_userId, recurringTransaction.Id, new YearMonth(2026, 9), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        var expectedId = TransactionId.ForRecurring(recurringTransaction.Id, new YearMonth(2026, 9));
        await _transactionRepository.Received(1).SaveAsync(
            Arg.Is<Transaction>(transaction => transaction.Id == expectedId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MaterializeAsync_DoesNotRecreateATransaction_ForAMonthTheUserExplicitlySkipped()
    {
        // Arrange -- e.g. the user deleted the September occurrence; it must not silently
        // reappear just because a bounded-range query for September ran again.
        var recurringTransaction = CreateValidRecurringTransaction().SkipMonth(new YearMonth(2026, 9));
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([recurringTransaction]);
        _transactionRepository
            .ExistsForRecurringTransactionAsync(_userId, recurringTransaction.Id, new YearMonth(2026, 9), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await _transactionRepository.DidNotReceive().SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MaterializeAsync_CapsMaterialization_AtTwelveMonthsIntoTheFuture()
    {
        // Arrange -- a recurrence active indefinitely, requested through a far-future date (e.g.
        // a caller passing an "all time" upper bound) must not generate years of not-yet-happened
        // transactions.
        var recurringTransaction = CreateValidRecurringTransaction();
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([recurringTransaction]);
        _transactionRepository
            .ExistsForRecurringTransactionAsync(_userId, recurringTransaction.Id, Arg.Any<YearMonth>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act -- request through a decade out.
        await _sut.MaterializeAsync(_userId, today, new DateOnly(today.Year + 10, 12, 31));

        // Assert -- capped at the current month plus twelve, inclusive: 13 months.
        const int expectedMaterializedMonths = 13;
        await _transactionRepository.Received(expectedMaterializedMonths).SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MaterializeAsync_GeneratesOneTransactionPerApplicableMonth_InAMultiMonthRange()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction(startDate: new DateOnly(2026, 1, 1));
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([recurringTransaction]);
        _transactionRepository
            .ExistsForRecurringTransactionAsync(_userId, recurringTransaction.Id, Arg.Any<YearMonth>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        // Assert
        await _transactionRepository.Received(3).SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    private RecurringTransaction CreateValidRecurringTransaction(
        int dayOfMonth = 5,
        DateOnly? startDate = null,
        bool active = true) => RecurringTransaction.Create(
        new RecurringTransactionId("recurring-1"),
        _userId,
        TransactionType.Expense,
        new Money(1800.00m),
        "Aluguel",
        new TagId("tag-moradia"),
        PaymentMethod.TransferenciaBancaria,
        dayOfMonth,
        startDate ?? new DateOnly(2026, 1, 1),
        null,
        active);
}
