using CashTrack.Domain.Common;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Common;

public sealed class YearMonthTest
{
    [Fact]
    public void Constructor_SetsYearAndMonth_WhenMonthIsValid()
    {
        // Act
        var yearMonth = new YearMonth(2026, 9);

        // Assert
        yearMonth.Year.Should().Be(2026);
        yearMonth.Month.Should().Be(9);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Constructor_Throws_WhenMonthIsOutOfRange(int month)
    {
        // Act
        var act = () => new YearMonth(2026, month);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromDate_ExtractsYearAndMonth_FromDate()
    {
        // Act
        var yearMonth = YearMonth.FromDate(new DateOnly(2026, 9, 10));

        // Assert
        yearMonth.Should().Be(new YearMonth(2026, 9));
    }

    [Fact]
    public void ToString_FormatsAsYyyyMm()
    {
        // Act
        var value = new YearMonth(2026, 9).ToString();

        // Assert
        value.Should().Be("2026-09");
    }

    [Fact]
    public void Parse_ReturnsYearMonth_ForValidYyyyMmString()
    {
        // Act
        var yearMonth = YearMonth.Parse("2026-09");

        // Assert
        yearMonth.Should().Be(new YearMonth(2026, 9));
    }

    [Theory]
    [InlineData("2026-9")]
    [InlineData("2026/09")]
    [InlineData("not-a-month")]
    [InlineData("")]
    public void Parse_Throws_ForInvalidString(string value)
    {
        // Act
        var act = () => YearMonth.Parse(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
