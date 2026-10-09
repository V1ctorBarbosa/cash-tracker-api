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

    [Fact]
    public void AddMonths_AdvancesWithinTheSameYear_WhenItDoesNotCrossADecemberBoundary()
    {
        // Act
        var result = new YearMonth(2026, 3).AddMonths(2);

        // Assert
        result.Should().Be(new YearMonth(2026, 5));
    }

    [Fact]
    public void AddMonths_RollsOverIntoTheNextYear()
    {
        // Act
        var result = new YearMonth(2026, 12).AddMonths(1);

        // Assert
        result.Should().Be(new YearMonth(2027, 1));
    }

    [Fact]
    public void AddMonths_RollsBackIntoThePreviousYear_ForNegativeMonths()
    {
        // Act
        var result = new YearMonth(2027, 1).AddMonths(-1);

        // Assert
        result.Should().Be(new YearMonth(2026, 12));
    }

    [Theory]
    [InlineData(2026, 5, 2026, 9, true)]
    [InlineData(2026, 9, 2026, 5, false)]
    [InlineData(2026, 9, 2026, 9, false)]
    public void LessThanOperator_ComparesYearThenMonth(int leftYear, int leftMonth, int rightYear, int rightMonth, bool expected)
    {
        // Act
        var result = new YearMonth(leftYear, leftMonth) < new YearMonth(rightYear, rightMonth);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void EnumerateRange_ReturnsEveryMonth_InclusiveOfBothEnds()
    {
        // Act
        var months = YearMonth.EnumerateRange(new YearMonth(2026, 11), new YearMonth(2027, 2)).ToList();

        // Assert
        months.Should().Equal(new YearMonth(2026, 11), new YearMonth(2026, 12), new YearMonth(2027, 1), new YearMonth(2027, 2));
    }

    [Fact]
    public void EnumerateRange_ReturnsSingleMonth_WhenFromEqualsTo()
    {
        // Act
        var months = YearMonth.EnumerateRange(new YearMonth(2026, 9), new YearMonth(2026, 9)).ToList();

        // Assert
        months.Should().Equal(new YearMonth(2026, 9));
    }

    [Fact]
    public void FirstDay_ReturnsTheFirstDayOfTheMonth()
    {
        // Act
        var firstDay = new YearMonth(2026, 9).FirstDay();

        // Assert
        firstDay.Should().Be(new DateOnly(2026, 9, 1));
    }

    [Theory]
    [InlineData(2026, 9, 30)]
    [InlineData(2026, 12, 31)]
    [InlineData(2026, 2, 28)]
    [InlineData(2028, 2, 29)]
    public void LastDay_ReturnsTheLastDayOfTheMonth(int year, int month, int expectedDay)
    {
        // Act
        var lastDay = new YearMonth(year, month).LastDay();

        // Assert
        lastDay.Should().Be(new DateOnly(year, month, expectedDay));
    }
}
