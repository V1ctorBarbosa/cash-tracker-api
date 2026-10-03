using CashTrack.Application.RecurringTransactions;
using CashTrack.Application.Tags;
using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace CashTrack.Tests.Application.RecurringTransactions;

public sealed class RecurringTransactionServiceTest
{
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = Substitute.For<IRecurringTransactionRepository>();
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly RecurringTransactionService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public RecurringTransactionServiceTest()
    {
        _sut = new RecurringTransactionService(_recurringTransactionRepository, _tagRepository);
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedDtos_FromRepository()
    {
        // Arrange
        var recurringTransaction = CreateValidRecurringTransaction();
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([recurringTransaction]);

        // Act
        var result = await _sut.ListAsync(_userId);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be("recurring-1");
        result[0].DayOfMonth.Should().Be(5);
    }

    [Fact]
    public async Task CreateAsync_SavesRecurringTransaction_ActiveByDefault()
    {
        // Arrange
        var request = new CreateRecurringTransactionRequest(
            "Expense", 1800.00m, "Aluguel", "tag-moradia", "TransferenciaBancaria", 5, new DateOnly(2026, 1, 1), null);

        // Act
        var result = await _sut.CreateAsync(_userId, request);

        // Assert
        result.Active.Should().BeTrue();
        result.Description.Should().Be("Aluguel");
        await _recurringTransactionRepository.Received(1).SaveAsync(
            Arg.Is<RecurringTransaction>(recurring => recurring.UserId == _userId && recurring.Active),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenTagIsUncategorized_AndDescriptionIsMissing()
    {
        // Arrange
        var tagId = new TagId("tag-uncategorized");
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns(Tag.CreateUncategorized(_userId, TransactionType.Expense));
        var request = new CreateRecurringTransactionRequest(
            "Expense", 1800.00m, null, "tag-uncategorized", "TransferenciaBancaria", 5, new DateOnly(2026, 1, 1), null);

        // Act
        var act = async () => await _sut.CreateAsync(_userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _recurringTransactionRepository.DidNotReceive().SaveAsync(Arg.Any<RecurringTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenRecurringTransactionDoesNotBelongToUser()
    {
        // Arrange
        var id = new RecurringTransactionId("recurring-1");
        _recurringTransactionRepository.GetByIdAsync(_userId, id, Arg.Any<CancellationToken>()).Returns((RecurringTransaction?)null);
        var request = new UpdateRecurringTransactionRequest(
            "Expense", 1800.00m, "Aluguel", "tag-moradia", "TransferenciaBancaria", 5, new DateOnly(2026, 1, 1), null, true);

        // Act
        var result = await _sut.UpdateAsync(_userId, id, request);

        // Assert
        result.Should().BeNull();
        await _recurringTransactionRepository.DidNotReceive().SaveAsync(Arg.Any<RecurringTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_CanPause_ByTogglingActiveToFalse()
    {
        // Arrange
        var id = new RecurringTransactionId("recurring-1");
        _recurringTransactionRepository.GetByIdAsync(_userId, id, Arg.Any<CancellationToken>()).Returns(CreateValidRecurringTransaction());
        var request = new UpdateRecurringTransactionRequest(
            "Expense", 1800.00m, "Aluguel", "tag-moradia", "TransferenciaBancaria", 5, new DateOnly(2026, 1, 1), null, false);

        // Act
        var result = await _sut.UpdateAsync(_userId, id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Active.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenTagIsUncategorized_AndDescriptionIsMissing()
    {
        // Arrange
        var id = new RecurringTransactionId("recurring-1");
        _recurringTransactionRepository.GetByIdAsync(_userId, id, Arg.Any<CancellationToken>()).Returns(CreateValidRecurringTransaction());
        var tagId = new TagId("tag-uncategorized");
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns(Tag.CreateUncategorized(_userId, TransactionType.Expense));
        var request = new UpdateRecurringTransactionRequest(
            "Expense", 1800.00m, null, "tag-uncategorized", "TransferenciaBancaria", 5, new DateOnly(2026, 1, 1), null, true);

        // Act
        var act = async () => await _sut.UpdateAsync(_userId, id, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _recurringTransactionRepository.DidNotReceive().SaveAsync(Arg.Any<RecurringTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_PreservesSkippedMonths_WhenEditingOtherFields()
    {
        // Arrange -- editing the amount/day/etc. of a recurrence must not silently un-skip a
        // month the user explicitly deleted an occurrence for.
        var id = new RecurringTransactionId("recurring-1");
        var existing = CreateValidRecurringTransaction().SkipMonth(new YearMonth(2026, 9));
        _recurringTransactionRepository.GetByIdAsync(_userId, id, Arg.Any<CancellationToken>()).Returns(existing);
        var request = new UpdateRecurringTransactionRequest(
            "Expense", 2000.00m, "Aluguel", "tag-moradia", "TransferenciaBancaria", 5, new DateOnly(2026, 1, 1), null, true);

        // Act
        var result = await _sut.UpdateAsync(_userId, id, request);

        // Assert
        result.Should().NotBeNull();
        await _recurringTransactionRepository.Received(1).SaveAsync(
            Arg.Is<RecurringTransaction>(recurring => recurring.SkippedMonths.Contains(new YearMonth(2026, 9))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipMonthAsync_SavesTheRecurringTransaction_WithTheMonthAddedToSkippedMonths()
    {
        // Arrange
        var id = new RecurringTransactionId("recurring-1");
        _recurringTransactionRepository.GetByIdAsync(_userId, id, Arg.Any<CancellationToken>()).Returns(CreateValidRecurringTransaction());

        // Act
        await _sut.SkipMonthAsync(_userId, id, new YearMonth(2026, 9));

        // Assert
        await _recurringTransactionRepository.Received(1).SaveAsync(
            Arg.Is<RecurringTransaction>(recurring => recurring.SkippedMonths.Contains(new YearMonth(2026, 9))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipMonthAsync_DoesNothing_WhenRecurringTransactionDoesNotBelongToUser()
    {
        // Arrange
        var id = new RecurringTransactionId("recurring-1");
        _recurringTransactionRepository.GetByIdAsync(_userId, id, Arg.Any<CancellationToken>()).Returns((RecurringTransaction?)null);

        // Act
        await _sut.SkipMonthAsync(_userId, id, new YearMonth(2026, 9));

        // Assert
        await _recurringTransactionRepository.DidNotReceive().SaveAsync(Arg.Any<RecurringTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenRecurringTransactionDoesNotBelongToUser()
    {
        // Arrange
        var id = new RecurringTransactionId("recurring-1");
        _recurringTransactionRepository.GetByIdAsync(_userId, id, Arg.Any<CancellationToken>()).Returns((RecurringTransaction?)null);

        // Act
        var result = await _sut.DeleteAsync(_userId, id);

        // Assert
        result.Should().BeFalse();
        await _recurringTransactionRepository.DidNotReceive().DeleteAsync(Arg.Any<UserId>(), Arg.Any<RecurringTransactionId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndDeletes_WhenRecurringTransactionBelongsToUser()
    {
        // Arrange
        var id = new RecurringTransactionId("recurring-1");
        _recurringTransactionRepository.GetByIdAsync(_userId, id, Arg.Any<CancellationToken>()).Returns(CreateValidRecurringTransaction());

        // Act
        var result = await _sut.DeleteAsync(_userId, id);

        // Assert
        result.Should().BeTrue();
        await _recurringTransactionRepository.Received(1).DeleteAsync(_userId, id, Arg.Any<CancellationToken>());
    }

    private RecurringTransaction CreateValidRecurringTransaction() => RecurringTransaction.Create(
        new RecurringTransactionId("recurring-1"),
        _userId,
        TransactionType.Expense,
        new Money(1800.00m),
        "Aluguel",
        new TagId("tag-moradia"),
        PaymentMethod.TransferenciaBancaria,
        5,
        new DateOnly(2026, 1, 1),
        null,
        true);
}
