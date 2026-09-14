using CashTrack.Domain.Users;

namespace CashTrack.Domain.Tags;

/// <summary>
/// A category the user assigns to a transaction, for grouping and analysis (Section 7 of the
/// implementation document). Used on both income and expense transactions.
/// </summary>
public sealed class Tag
{
    public TagId Id { get; }

    public UserId UserId { get; }

    public string Name { get; }

    public TagColor Color { get; }

    private Tag(TagId id, UserId userId, string name, TagColor color)
    {
        Id = id;
        UserId = userId;
        Name = name;
        Color = color;
    }

    public static Tag Create(TagId id, UserId userId, string name, TagColor color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        return new Tag(id, userId, name.Trim(), color);
    }
}
