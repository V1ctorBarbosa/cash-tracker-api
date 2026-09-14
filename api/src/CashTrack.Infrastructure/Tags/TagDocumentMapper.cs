using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Infrastructure.Tags;

/// <summary>
/// Pure mapping between the domain's <see cref="Tag"/> entity and its Firestore persistence
/// representation. Kept free of any Firestore I/O so it can be unit tested without a live database.
/// </summary>
internal static class TagDocumentMapper
{
    public static Tag ToDomain(TagId id, TagDocument document) => Tag.Create(
        id,
        new UserId(document.UserId),
        document.Name,
        new TagColor(document.Color),
        Enum.Parse<TransactionType>(document.Kind));

    public static TagDocument ToDocument(Tag tag) => new()
    {
        UserId = tag.UserId.Value,
        Name = tag.Name,
        Color = tag.Color.Value,
        Kind = tag.Kind.ToString(),
    };
}
