using System.Globalization;

namespace CashTrack.Domain.Common;

/// <summary>
/// A calendar month, independent of any specific day -- the granularity Budgets and recurring
/// transaction generation operate at (Sections 12-13 of the implementation document).
/// </summary>
public readonly record struct YearMonth
{
    private const string Format = "yyyy-MM";

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

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
