using Google.Cloud.Firestore;

namespace CashTrack.Infrastructure.Users;

[FirestoreData]
internal sealed class UserDocument
{
    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Email { get; set; } = string.Empty;
}
