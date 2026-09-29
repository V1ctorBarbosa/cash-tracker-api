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
    /// <summary>
    /// Fixed display name for every user's system-defined "uncategorized" tag (Section 7):
    /// dedicated so a transaction skipping category selection can still be identified in charts
    /// without forcing a category that doesn't fit.
    /// </summary>
    private const string UncategorizedTagName = "Não categorizado";

    /// <summary>Deliberately outside <c>TagColorGroups</c> (the user-assignable palette), so a chart segment for "uncategorized" is never confused with a user-created tag.</summary>
    private static readonly TagColor UncategorizedTagColor = new("#6F6A5E");

    public TagId Id { get; }

    public UserId UserId { get; }

    public string Name { get; }

    public TagColor Color { get; }

    public TransactionType Kind { get; }

    /// <summary>True for the one auto-provisioned "uncategorized" tag per kind -- such a tag can't be renamed, recolored, or deleted by the user.</summary>
    public bool IsSystemDefined { get; }

    private Tag(TagId id, UserId userId, string name, TagColor color, TransactionType kind, bool isSystemDefined)
    {
        Id = id;
        UserId = userId;
        Name = name;
        Color = color;
        Kind = kind;
        IsSystemDefined = isSystemDefined;
    }

    public static Tag Create(TagId id, UserId userId, string name, TagColor color, TransactionType kind)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        return new Tag(id, userId, name.Trim(), color, kind, isSystemDefined: false);
    }

    public static Tag CreateUncategorized(UserId userId, TransactionType kind) =>
        new(TagId.ForUncategorized(userId, kind), userId, UncategorizedTagName, UncategorizedTagColor, kind, isSystemDefined: true);
}
