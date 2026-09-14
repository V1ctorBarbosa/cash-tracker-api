using CashTrack.Domain.Users;

namespace CashTrack.Infrastructure.Users;

/// <summary>
/// Pure mapping between the domain's <see cref="User"/> entity and its Firestore persistence
/// representation. Kept free of any Firestore I/O so it can be unit tested without a live database.
/// </summary>
internal static class UserDocumentMapper
{
    public static User ToDomain(UserId id, UserDocument document) => User.Create(id, document.Name, document.Email);

    public static UserDocument ToDocument(User user) => new()
    {
        Name = user.Name,
        Email = user.Email,
    };
}
