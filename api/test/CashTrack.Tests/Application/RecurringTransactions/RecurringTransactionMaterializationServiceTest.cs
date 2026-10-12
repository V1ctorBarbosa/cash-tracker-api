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
    private readonly List<Transaction> _savedTransactions = [];

    public RecurringTransactionMaterializationServiceTest()
    {
        _sut = new RecurringTransactionMaterializationService(
            _recurringTransactionRepository,
            _transactionRepository,
            NullLogger<RecurringTransactionMaterializationService>.Instance);

        GivenMaterializedOccurrences();
        _transactionRepository
            .SaveManyAsync(Arg.Do<IReadOnlyCollection<Transaction>>(transactions => _savedTransactions.AddRange(transactions)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task MaterializeAsync_DoesNothing_WhenUserHasNoRecurringTransactions()
    {
        // Arrange
        GivenRecurringTransactions();

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await ThenNothingWasQueriedOrSavedAsync();
    }

    [Fact]
    public async Task MaterializeAsync_DoesNothing_WhenRecurringTransactionDoesNotApplyToAnyMonthInRange()
    {
        // Arrange -- inactive, so AppliesToMonth is always false
        GivenRecurringTransactions(CreateValidRecurringTransaction(active: false));

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await ThenNothingWasQueriedOrSavedAsync();
    }

    [Fact]
    public async Task MaterializeAsync_DoesNotSave_WhenATransactionAlreadyExistsForThatMonth()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction();
        GivenRecurringTransactions(recurringTransaction);
        GivenMaterializedOccurrences(new RecurringOccurrence(recurringTransaction.Id, new YearMonth(2026, 9)));

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await _transactionRepository.DidNotReceive().SaveManyAsync(Arg.Any<IReadOnlyCollection<Transaction>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MaterializeAsync_SavesTransaction_LinkedToTheRecurringDefinition_WhenNoneExistsYet()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction(dayOfMonth: 5);
        GivenRecurringTransactions(recurringTransaction);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        var savedTransaction = _savedTransactions.Should().ContainSingle().Subject;
        savedTransaction.UserId.Should().Be(_userId);
        savedTransaction.RecurringTransactionId.Should().Be(recurringTransaction.Id);
        savedTransaction.Date.Should().Be(new DateOnly(2026, 9, 5));
        savedTransaction.Description.Should().Be(recurringTransaction.Description);
        savedTransaction.Amount.Should().Be(recurringTransaction.Amount);
    }

    [Fact]
    public async Task MaterializeAsync_ClampsTheGeneratedDate_ToTheMonthsLastDay()
    {
        // Arrange -- day 31 in February
        GivenRecurringTransactions(CreateValidRecurringTransaction(dayOfMonth: 31, startDate: new DateOnly(2026, 1, 1)));

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));

        // Assert
        _savedTransactions.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public async Task MaterializeAsync_UsesADeterministicTransactionId_DerivedFromTheRecurringTransactionAndMonth()
    {
        // Arrange -- a random id would let two concurrent materialization calls (e.g. the
        // dashboard's month and trailing-range queries firing together) each write their own
        // transaction for the same (recurring, month) pair, double-counting the amount.
        var recurringTransaction = CreateValidRecurringTransaction(dayOfMonth: 5);
        GivenRecurringTransactions(recurringTransaction);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        var expectedId = TransactionId.ForRecurring(recurringTransaction.Id, new YearMonth(2026, 9));
        _savedTransactions.Should().ContainSingle().Which.Id.Should().Be(expectedId);
    }

    [Fact]
    public async Task MaterializeAsync_DoesNotRecreateATransaction_ForAMonthTheUserExplicitlySkipped()
    {
        // Arrange -- e.g. the user deleted the September occurrence; it must not silently
        // reappear just because a bounded-range query for September ran again.
        GivenRecurringTransactions(CreateValidRecurringTransaction().SkipMonth(new YearMonth(2026, 9)));

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await ThenNothingWasQueriedOrSavedAsync();
    }

    [Fact]
    public async Task MaterializeAsync_CapsMaterialization_AtTwelveMonthsIntoTheFuture()
    {
        // Arrange -- a recurrence active indefinitely, requested through a far-future date (e.g.
        // a caller passing an "all time" upper bound) must not generate years of not-yet-happened
        // transactions.
        GivenRecurringTransactions(CreateValidRecurringTransaction());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act -- request through a decade out.
        await _sut.MaterializeAsync(_userId, today, new DateOnly(today.Year + 10, 12, 31));

        // Assert -- capped at the current month plus twelve, inclusive: 13 months.
        const int expectedMaterializedMonths = 13;
        _savedTransactions.Should().HaveCount(expectedMaterializedMonths);
        await _transactionRepository.Received(1).ListMaterializedOccurrencesAsync(
            _userId,
            Arg.Any<IReadOnlyCollection<RecurringTransactionId>>(),
            YearMonth.FromDate(today),
            YearMonth.FromDate(today).AddMonths(12),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MaterializeAsync_GeneratesOneTransactionPerApplicableMonth_InAMultiMonthRange()
    {
        // Arrange
        GivenRecurringTransactions(CreateValidRecurringTransaction(startDate: new DateOnly(2026, 1, 1)));

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        // Assert
        _savedTransactions.Select(transaction => transaction.Date.Month).Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public async Task MaterializeAsync_SavesOnlyTheMissingOccurrences_WhenSomeMonthsAreAlreadyMaterialized()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction(startDate: new DateOnly(2026, 1, 1));
        GivenRecurringTransactions(recurringTransaction);
        GivenMaterializedOccurrences(
            new RecurringOccurrence(recurringTransaction.Id, new YearMonth(2026, 1)),
            new RecurringOccurrence(recurringTransaction.Id, new YearMonth(2026, 3)));

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        // Assert
        _savedTransactions.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 2, 5));
    }

    [Fact]
    public async Task MaterializeAsync_QueriesExistingOccurrencesOnce_AndSavesInOneCall_ForEveryRecurrenceAndMonth()
    {
        // Arrange -- six months across three recurrences used to cost eighteen sequential
        // existence queries plus eighteen individual writes.
        var rent = CreateValidRecurringTransaction(id: "recurring-rent");
        var internet = CreateValidRecurringTransaction(id: "recurring-internet");
        var salary = CreateValidRecurringTransaction(id: "recurring-salary");
        GivenRecurringTransactions(rent, internet, salary);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 4, 1), new DateOnly(2026, 9, 30));

        // Assert
        await _transactionRepository.Received(1).ListMaterializedOccurrencesAsync(
            _userId,
            Arg.Is<IReadOnlyCollection<RecurringTransactionId>>(recurringTransactionIds =>
                recurringTransactionIds.Count == 3
                && recurringTransactionIds.Contains(rent.Id)
                && recurringTransactionIds.Contains(internet.Id)
                && recurringTransactionIds.Contains(salary.Id)),
            new YearMonth(2026, 4),
            new YearMonth(2026, 9),
            Arg.Any<CancellationToken>());
        await _transactionRepository.Received(1).SaveManyAsync(Arg.Any<IReadOnlyCollection<Transaction>>(), Arg.Any<CancellationToken>());
        _savedTransactions.Should().HaveCount(18);
    }

    [Fact]
    public async Task MaterializeAsync_OnlyQueriesTheRecurrencesDueInTheRange()
    {
        // Arrange
        var due = CreateValidRecurringTransaction(id: "recurring-due");
        var inactive = CreateValidRecurringTransaction(id: "recurring-inactive", active: false);
        GivenRecurringTransactions(due, inactive);

        // Act
        await _sut.MaterializeAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await _transactionRepository.Received(1).ListMaterializedOccurrencesAsync(
            _userId,
            Arg.Is<IReadOnlyCollection<RecurringTransactionId>>(recurringTransactionIds =>
                recurringTransactionIds.Count == 1 && recurringTransactionIds.Contains(due.Id)),
            Arg.Any<YearMonth>(),
            Arg.Any<YearMonth>(),
            Arg.Any<CancellationToken>());
    }

    private void GivenRecurringTransactions(params RecurringTransaction[] recurringTransactions) =>
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns(recurringTransactions);

    private void GivenMaterializedOccurrences(params RecurringOccurrence[] occurrences) =>
        _transactionRepository
            .ListMaterializedOccurrencesAsync(
                _userId,
                Arg.Any<IReadOnlyCollection<RecurringTransactionId>>(),
                Arg.Any<YearMonth>(),
                Arg.Any<YearMonth>(),
                Arg.Any<CancellationToken>())
            .Returns(occurrences.ToHashSet());

    private async Task ThenNothingWasQueriedOrSavedAsync()
    {
        await _transactionRepository.DidNotReceive().ListMaterializedOccurrencesAsync(
            Arg.Any<UserId>(),
            Arg.Any<IReadOnlyCollection<RecurringTransactionId>>(),
            Arg.Any<YearMonth>(),
            Arg.Any<YearMonth>(),
            Arg.Any<CancellationToken>());
        await _transactionRepository.DidNotReceive().SaveManyAsync(Arg.Any<IReadOnlyCollection<Transaction>>(), Arg.Any<CancellationToken>());
    }

    private RecurringTransaction CreateValidRecurringTransaction(
        string id = "recurring-1",
        int dayOfMonth = 5,
        DateOnly? startDate = null,
        bool active = true) => RecurringTransaction.Create(
        new RecurringTransactionId(id),
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
