using CashTrack.Infrastructure.Common;
using FluentAssertions;

namespace CashTrack.Tests.Infrastructure.Common;

public sealed class DecimalConverterTest
{
    private readonly DecimalConverter _sut = new();

    [Theory]
    [InlineData(89.90, 8990L)]
    [InlineData(1800.00, 180000L)]
    [InlineData(0.05, 5L)]
    [InlineData(0, 0L)]
    public void ToFirestore_ConvertsToIntegerCents(decimal amount, long expectedCents)
    {
        // Act
        var stored = _sut.ToFirestore(amount);

        // Assert
        stored.Should().Be(expectedCents);
    }

    [Fact]
    public void FromFirestore_ConvertsIntegerCents_BackToDecimal()
    {
        // Act
        var amount = _sut.FromFirestore(8990L);

        // Assert
        amount.Should().Be(89.90m);
    }

    [Fact]
    public void FromFirestore_TreatsADoubleValue_AsCentsToo()
    {
        // Act
        var amount = _sut.FromFirestore(8990.0);

        // Assert
        amount.Should().Be(89.90m);
    }

    [Fact]
    public void RoundTrips_WithoutLosingPrecision()
    {
        // Arrange
        var original = 2929.20m;

        // Act
        var roundTripped = _sut.FromFirestore(_sut.ToFirestore(original));

        // Assert
        roundTripped.Should().Be(original);
    }

    [Fact]
    public void FromFirestore_Throws_ForAnUnsupportedType()
    {
        // Act
        var act = () => _sut.FromFirestore("8990");

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
