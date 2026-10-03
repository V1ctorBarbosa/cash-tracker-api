using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Domain.Tags;

public readonly record struct TagId
{
    public string Value { get; }

    public TagId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tag id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Deterministic id for a user's system-defined "uncategorized" tag of a given kind, so the
    /// tag can be provisioned lazily and re-provisioned safely after deletion without ever
    /// colliding with a user-created tag's random id.
    /// </summary>
    public static TagId ForUncategorized(UserId userId, TransactionType kind) => new($"uncategorized_{userId.Value}_{kind}");
}
