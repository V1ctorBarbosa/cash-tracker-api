using CashTrack.Application.Tags;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;
using Google.Cloud.Firestore;

namespace CashTrack.Infrastructure.Tags;

public sealed class FirestoreTagRepository : ITagRepository
{
    private const string CollectionName = "tags";
    private const string UserIdFieldName = "UserId";

    private readonly FirestoreDb _firestoreDb;

    public FirestoreTagRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<IReadOnlyList<Tag>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var querySnapshot = await _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(UserIdFieldName, userId.Value)
            .GetSnapshotAsync(cancellationToken);

        return querySnapshot.Documents
            .Select(document => TagDocumentMapper.ToDomain(new TagId(document.Id), document.ConvertTo<TagDocument>()))
            .ToList();
    }

    public async Task<Tag?> GetByIdAsync(UserId userId, TagId tagId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _firestoreDb.Collection(CollectionName)
            .Document(tagId.Value)
            .GetSnapshotAsync(cancellationToken);

        if (!snapshot.Exists)
        {
            return null;
        }

        var document = snapshot.ConvertTo<TagDocument>();
        if (document.UserId != userId.Value)
        {
            return null;
        }

        return TagDocumentMapper.ToDomain(tagId, document);
    }

    public async Task SaveAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        var document = TagDocumentMapper.ToDocument(tag);

        await _firestoreDb.Collection(CollectionName)
            .Document(tag.Id.Value)
            .SetAsync(document, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(UserId userId, TagId tagId, CancellationToken cancellationToken = default)
    {
        await _firestoreDb.Collection(CollectionName)
            .Document(tagId.Value)
            .DeleteAsync(cancellationToken: cancellationToken);
    }
}
