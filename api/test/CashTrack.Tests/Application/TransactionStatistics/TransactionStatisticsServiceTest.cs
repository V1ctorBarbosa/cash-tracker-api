using CashTrack.Application.Tags;
using CashTrack.Application.Transactions;
using CashTrack.Application.TransactionStatistics;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace CashTrack.Tests.Application.TransactionStatistics;

public sealed class TransactionStatisticsServiceTest
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly TransactionStatisticsService _sut;
    private readonly UserId _userId = new("firebase-uid-123");
    private readonly TagId _moradiaTagId = new("tag-moradia");
    private readonly TagId _lazerTagId = new("tag-lazer");

    public TransactionStatisticsServiceTest()
    {
        _sut = new TransactionStatisticsService(_transactionRepository, _tagRepository);

        _tagRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([
            Tag.Create(_moradiaTagId, _userId, "Moradia", new TagColor("#000000"), TransactionType.Expense),
            Tag.Create(_lazerTagId, _userId, "Lazer", new TagColor("#000000"), TransactionType.Expense),
        ]);
    }

    [Fact]
    public async Task GetAsync_ReturnsTheTotalCountAndTheFirstTransactionDate()
    {
        // Arrange
        _transactionRepository.CountByUserAsync(_userId, null, Arg.Any<CancellationToken>()).Returns(42);
        _transactionRepository.FindEarliestDateAsync(_userId, Arg.Any<CancellationToken>()).Returns(new DateOnly(2025, 11, 3));

        // Act
        var statistics = await _sut.GetAsync(_userId);

        // Assert
        statistics.TransactionCount.Should().Be(42);
        statistics.FirstTransactionDate.Should().Be(new DateOnly(2025, 11, 3));
    }

    [Fact]
    public async Task GetAsync_CountsUsesForEveryTag_IncludingTagsNeverUsed()
    {
        // Arrange
        _transactionRepository.CountByUserAsync(_userId, _moradiaTagId, Arg.Any<CancellationToken>()).Returns(12);
        _transactionRepository.CountByUserAsync(_userId, _lazerTagId, Arg.Any<CancellationToken>()).Returns(0);

        // Act
        var statistics = await _sut.GetAsync(_userId);

        // Assert
        statistics.TagUsages.Should().BeEquivalentTo(
        [
            new TagUsageDto("tag-moradia", 12),
            new TagUsageDto("tag-lazer", 0),
        ]);
    }

    [Fact]
    public async Task GetAsync_ReturnsZeroAndNoFirstDate_WhenTheUserHasNoTransactions()
    {
        // Arrange
        _transactionRepository.CountByUserAsync(_userId, Arg.Any<TagId?>(), Arg.Any<CancellationToken>()).Returns(0);
        _transactionRepository.FindEarliestDateAsync(_userId, Arg.Any<CancellationToken>()).Returns((DateOnly?)null);

        // Act
        var statistics = await _sut.GetAsync(_userId);

        // Assert
        statistics.TransactionCount.Should().Be(0);
        statistics.FirstTransactionDate.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_NeverLoadsTheTransactionsThemselves()
    {
        // Act
        await _sut.GetAsync(_userId);

        // Assert
        await _transactionRepository.DidNotReceive().ListByUserAsync(Arg.Any<UserId>(), Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>());
    }
}
