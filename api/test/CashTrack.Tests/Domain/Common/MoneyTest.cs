using CashTrack.Domain.Common;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Common;

public sealed class MoneyTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(89.90)]
    [InlineData(5200)]
    public void Constructor_SetsAmount_WhenNonNegative(decimal amount)
    {
        // Act
        var money = new Money(amount);

        // Assert
        money.Amount.Should().Be(amount);
    }

    [Fact]
    public void Constructor_Throws_WhenAmountIsNegative()
    {
        // Act
        var act = () => new Money(-0.01m);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
