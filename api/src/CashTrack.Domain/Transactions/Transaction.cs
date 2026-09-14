using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;

namespace CashTrack.Domain.Transactions;

/// <summary>
/// A single financial movement -- an income or an expense (Section 9 of the implementation
/// document). Both share every field and query shape, so they are one entity with a
/// <see cref="TransactionType"/> discriminator rather than two near-identical entities.
/// </summary>
public sealed class Transaction
{
    public TransactionId Id { get; }

    public UserId UserId { get; }

    public TransactionType Type { get; }

    public Money Amount { get; }

    public DateOnly Date { get; }

    public string Description { get; }

    public TagId TagId { get; }

    public PaymentMethod PaymentMethod { get; }

    public DateTimeOffset CreatedAt { get; }

    private Transaction(
        TransactionId id,
        UserId userId,
        TransactionType type,
        Money amount,
        DateOnly date,
        string description,
        TagId tagId,
        PaymentMethod paymentMethod,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Type = type;
        Amount = amount;
        Date = date;
        Description = description;
        TagId = tagId;
        PaymentMethod = paymentMethod;
        CreatedAt = createdAt;
    }

    public static Transaction Create(
        TransactionId id,
        UserId userId,
        TransactionType type,
        Money amount,
        DateOnly date,
        string description,
        TagId tagId,
        PaymentMethod paymentMethod,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be empty.", nameof(description));
        }

        return new Transaction(id, userId, type, amount, date, description.Trim(), tagId, paymentMethod, createdAt);
    }
}
