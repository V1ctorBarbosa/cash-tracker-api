namespace CashTrack.Application.RecurringTransactions;

public sealed record RecurringTransactionDto(
    string Id,
    string Type,
    decimal Amount,
    string Description,
    string TagId,
    string PaymentMethod,
    int DayOfMonth,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool Active);
