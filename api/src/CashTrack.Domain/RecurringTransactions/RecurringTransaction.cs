using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Domain.RecurringTransactions;

/// <summary>
/// A transaction definition that repeats every month -- rent, subscriptions, a recurring salary
/// (Section 11 of the implementation document). Materialized lazily into real <see cref="Transaction"/>
/// records per month (Section 12); this entity only holds the recurrence definition itself.
/// </summary>
public sealed class RecurringTransaction
{
    private const int MinDayOfMonth = 1;
    private const int MaxDayOfMonth = 31;

    public RecurringTransactionId Id { get; }

    public UserId UserId { get; }

    public TransactionType Type { get; }

    public Money Amount { get; }

    public string Description { get; }

    public TagId TagId { get; }

    public PaymentMethod PaymentMethod { get; }

    public int DayOfMonth { get; }

    public DateOnly StartDate { get; }

    public DateOnly? EndDate { get; }

    public bool Active { get; }

    private RecurringTransaction(
        RecurringTransactionId id,
        UserId userId,
        TransactionType type,
        Money amount,
        string description,
        TagId tagId,
        PaymentMethod paymentMethod,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool active)
    {
        Id = id;
        UserId = userId;
        Type = type;
        Amount = amount;
        Description = description;
        TagId = tagId;
        PaymentMethod = paymentMethod;
        DayOfMonth = dayOfMonth;
        StartDate = startDate;
        EndDate = endDate;
        Active = active;
    }

    public static RecurringTransaction Create(
        RecurringTransactionId id,
        UserId userId,
        TransactionType type,
        Money amount,
        string description,
        TagId tagId,
        PaymentMethod paymentMethod,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool active)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be empty.", nameof(description));
        }

        if (dayOfMonth is < MinDayOfMonth or > MaxDayOfMonth)
        {
            throw new ArgumentOutOfRangeException(nameof(dayOfMonth), dayOfMonth, $"Day of month must be between {MinDayOfMonth} and {MaxDayOfMonth}.");
        }

        if (endDate is { } end && end < startDate)
        {
            throw new ArgumentException("End date cannot be before the start date.", nameof(endDate));
        }

        return new RecurringTransaction(id, userId, type, amount, description.Trim(), tagId, paymentMethod, dayOfMonth, startDate, endDate, active);
    }

    /// <summary>
    /// Whether this recurrence is due to generate a transaction for the given month -- active,
    /// and the month falls within [StartDate, EndDate].
    /// </summary>
    public bool AppliesToMonth(YearMonth month)
    {
        if (!Active)
        {
            return false;
        }

        var monthEnd = new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
        if (StartDate > monthEnd)
        {
            return false;
        }

        return EndDate is not { } endDate || endDate >= new DateOnly(month.Year, month.Month, 1);
    }

    /// <summary>
    /// The date a transaction generated for the given month should carry -- <see cref="DayOfMonth"/>,
    /// clamped to that month's last day (e.g. day 31 becomes day 28/29 in February).
    /// </summary>
    public DateOnly GetDateForMonth(YearMonth month)
    {
        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
        return new DateOnly(month.Year, month.Month, Math.Min(DayOfMonth, daysInMonth));
    }
}
