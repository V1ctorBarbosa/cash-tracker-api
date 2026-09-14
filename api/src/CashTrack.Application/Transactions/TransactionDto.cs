namespace CashTrack.Application.Transactions;

public sealed record TransactionDto(
    string Id,
    string Type,
    decimal Amount,
    DateOnly Date,
    string Description,
    string TagId,
    string PaymentMethod,
    string? RecurringTransactionId,
    DateTimeOffset CreatedAt);
