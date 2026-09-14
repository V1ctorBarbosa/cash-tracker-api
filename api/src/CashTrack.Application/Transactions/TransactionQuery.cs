using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;

namespace CashTrack.Application.Transactions;

/// <summary>
/// Optional filters for listing transactions (Section 18 of the implementation document).
/// A null field means that filter is not applied.
/// </summary>
public sealed record TransactionQuery(
    DateOnly? From,
    DateOnly? To,
    TagId? TagId,
    TransactionType? Type,
    PaymentMethod? PaymentMethod);
