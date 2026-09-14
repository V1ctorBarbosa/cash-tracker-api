using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;
using CashTrack.Infrastructure.Budgets;
using FluentAssertions;

namespace CashTrack.Tests.Infrastructure.Budgets;

public sealed class BudgetDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromBudget()
    {
        // Arrange
        var budget = Budget.Create(
            new UserId("firebase-uid-123"),
            new TagId("tag-lazer"),
            new YearMonth(2026, 9),
            new Money(500.00m));

        // Act
        var document = BudgetDocumentMapper.ToDocument(budget);

        // Assert
        document.UserId.Should().Be("firebase-uid-123");
        document.TagId.Should().Be("tag-lazer");
        document.Month.Should().Be("2026-09");
        document.LimitAmount.Should().Be(500.00m);
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument_WithTheSameDeterministicId()
    {
        // Arrange
        var originalBudget = Budget.Create(
            new UserId("firebase-uid-123"),
            new TagId("tag-lazer"),
            new YearMonth(2026, 9),
            new Money(500.00m));

        // Act
        var document = BudgetDocumentMapper.ToDocument(originalBudget);
        var roundTripped = BudgetDocumentMapper.ToDomain(document);

        // Assert
        roundTripped.Id.Should().Be(originalBudget.Id);
        roundTripped.UserId.Should().Be(originalBudget.UserId);
        roundTripped.TagId.Should().Be(originalBudget.TagId);
        roundTripped.Month.Should().Be(originalBudget.Month);
        roundTripped.LimitAmount.Should().Be(originalBudget.LimitAmount);
    }
}
