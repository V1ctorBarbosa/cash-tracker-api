namespace CashTrack.Application.Transactions;

public sealed record UpdateTransactionRequest(
    string Type,
    decimal Amount,
    DateOnly Date,
    string Description,
    string TagId,
    string PaymentMethod);
