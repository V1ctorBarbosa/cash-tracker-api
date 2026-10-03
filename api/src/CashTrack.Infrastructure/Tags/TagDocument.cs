using Google.Cloud.Firestore;

namespace CashTrack.Infrastructure.Tags;

[FirestoreData]
internal sealed class TagDocument
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Color { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Kind { get; set; } = string.Empty;

    [FirestoreProperty]
    public bool IsSystemDefined { get; set; }
}
