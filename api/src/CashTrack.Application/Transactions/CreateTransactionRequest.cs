namespace CashTrack.Application.Transactions;

public sealed record CreateTransactionRequest(
    string Type,
    decimal Amount,
    DateOnly Date,
    string? Description,
    string TagId,
    string PaymentMethod);
