using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Domain.Tags;

/// <summary>
/// A category the user assigns to a transaction, for grouping and analysis (Section 7 of the
/// implementation document). A tag is scoped to one <see cref="TransactionType"/> -- an income
/// source and an expense category are different concepts even when named alike, so a tag never
/// shows up as a choice for the other kind of transaction.
/// </summary>
public sealed class Tag
{
    public TagId Id { get; }

    public UserId UserId { get; }

    public string Name { get; }

    public TagColor Color { get; }

    public TransactionType Kind { get; }

    private Tag(TagId id, UserId userId, string name, TagColor color, TransactionType kind)
    {
        Id = id;
        UserId = userId;
        Name = name;
        Color = color;
        Kind = kind;
    }

    public static Tag Create(TagId id, UserId userId, string name, TagColor color, TransactionType kind)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        return new Tag(id, userId, name.Trim(), color, kind);
    }
}
