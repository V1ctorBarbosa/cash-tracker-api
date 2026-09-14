using CashTrack.Application.Budgets;
using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace CashTrack.Tests.Application.Budgets;

public sealed class BudgetServiceTest
{
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly BudgetService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public BudgetServiceTest()
    {
        _sut = new BudgetService(_budgetRepository);
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedDtos_FilteredByMonth_WhenMonthIsGiven()
    {
        // Arrange
        var budget = Budget.Create(_userId, new TagId("tag-lazer"), new YearMonth(2026, 9), new Money(500.00m));
        _budgetRepository.ListByUserAsync(_userId, new YearMonth(2026, 9), Arg.Any<CancellationToken>()).Returns([budget]);

        // Act
        var result = await _sut.ListAsync(_userId, "2026-09");

        // Assert
        result.Should().ContainSingle();
        result[0].TagId.Should().Be("tag-lazer");
        result[0].Month.Should().Be("2026-09");
        result[0].LimitAmount.Should().Be(500.00m);
    }

    [Fact]
    public async Task ListAsync_PassesNullMonth_WhenMonthIsNotGiven()
    {
        // Arrange
        _budgetRepository.ListByUserAsync(_userId, null, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _sut.ListAsync(_userId, month: null);

        // Assert
        await _budgetRepository.Received(1).ListByUserAsync(_userId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertAsync_SavesBudget_WithDeterministicId_ForTagAndMonth()
    {
        // Arrange
        var request = new UpsertBudgetRequest(500.00m);

        // Act
        var result = await _sut.UpsertAsync(_userId, "tag-lazer", "2026-09", request);

        // Assert
        result.Id.Should().Be(BudgetId.For(_userId, new TagId("tag-lazer"), new YearMonth(2026, 9)).Value);
        result.LimitAmount.Should().Be(500.00m);
        await _budgetRepository.Received(1).SaveAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenBudgetDoesNotExist()
    {
        // Arrange
        _budgetRepository.GetByIdAsync(_userId, Arg.Any<BudgetId>(), Arg.Any<CancellationToken>()).Returns((Budget?)null);

        // Act
        var result = await _sut.DeleteAsync(_userId, "tag-lazer", "2026-09");

        // Assert
        result.Should().BeFalse();
        await _budgetRepository.DidNotReceive().DeleteAsync(Arg.Any<UserId>(), Arg.Any<BudgetId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndDeletes_WhenBudgetExists()
    {
        // Arrange
        var budget = Budget.Create(_userId, new TagId("tag-lazer"), new YearMonth(2026, 9), new Money(500.00m));
        _budgetRepository.GetByIdAsync(_userId, budget.Id, Arg.Any<CancellationToken>()).Returns(budget);

        // Act
        var result = await _sut.DeleteAsync(_userId, "tag-lazer", "2026-09");

        // Assert
        result.Should().BeTrue();
        await _budgetRepository.Received(1).DeleteAsync(_userId, budget.Id, Arg.Any<CancellationToken>());
    }
}
