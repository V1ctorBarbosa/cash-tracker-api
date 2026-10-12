using System.Globalization;

namespace CashTrack.Domain.Common;

/// <summary>
/// A calendar month, independent of any specific day -- the granularity Budgets and recurring
/// transaction generation operate at (Sections 12-13 of the implementation document).
/// </summary>
public readonly record struct YearMonth : IComparable<YearMonth>
{
    private const string Format = "yyyy-MM";
    private const int MonthsPerYear = 12;

    public int Year { get; }

    public int Month { get; }

    public YearMonth(int year, int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
        }

        Year = year;
        Month = month;
    }

    public static YearMonth FromDate(DateOnly date) => new(date.Year, date.Month);

    public static YearMonth Parse(string value)
    {
        if (!DateOnly.TryParseExact($"{value}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new ArgumentException($"'{value}' is not a valid year-month (expected {Format}).", nameof(value));
        }

        return FromDate(parsed);
    }

    /// <summary>Every month from <paramref name="from"/> to <paramref name="to"/>, inclusive on both ends.</summary>
    public static IEnumerable<YearMonth> EnumerateRange(YearMonth from, YearMonth to)
    {
        var current = from;
        while (current <= to)
        {
            yield return current;
            current = current.AddMonths(1);
        }
    }

    public YearMonth AddMonths(int months)
    {
        var totalMonths = (Year * MonthsPerYear) + (Month - 1) + months;
        return new YearMonth(totalMonths / MonthsPerYear, (totalMonths % MonthsPerYear) + 1);
    }

    public DateOnly FirstDay() => new(Year, Month, 1);

    public DateOnly LastDay() => new(Year, Month, DateTime.DaysInMonth(Year, Month));

    public int CompareTo(YearMonth other) => (Year, Month).CompareTo((other.Year, other.Month));

    public static bool operator <(YearMonth left, YearMonth right) => left.CompareTo(right) < 0;

    public static bool operator >(YearMonth left, YearMonth right) => left.CompareTo(right) > 0;

    public static bool operator <=(YearMonth left, YearMonth right) => left.CompareTo(right) <= 0;

    public static bool operator >=(YearMonth left, YearMonth right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
