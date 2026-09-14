using CashTrack.Application.RecurringTransactions;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Users;
using Google.Cloud.Firestore;

namespace CashTrack.Infrastructure.RecurringTransactions;

public sealed class FirestoreRecurringTransactionRepository : IRecurringTransactionRepository
{
    private const string CollectionName = "recurringTransactions";
    private const string UserIdFieldName = "UserId";

    private readonly FirestoreDb _firestoreDb;

    public FirestoreRecurringTransactionRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<IReadOnlyList<RecurringTransaction>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var querySnapshot = await _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(UserIdFieldName, userId.Value)
            .GetSnapshotAsync(cancellationToken);

        return querySnapshot.Documents
            .Select(document => RecurringTransactionDocumentMapper.ToDomain(
                new RecurringTransactionId(document.Id),
                document.ConvertTo<RecurringTransactionDocument>()))
            .ToList();
    }

    public async Task<RecurringTransaction?> GetByIdAsync(
        UserId userId,
        RecurringTransactionId recurringTransactionId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _firestoreDb.Collection(CollectionName)
            .Document(recurringTransactionId.Value)
            .GetSnapshotAsync(cancellationToken);

        if (!snapshot.Exists)
        {
            return null;
        }

        var document = snapshot.ConvertTo<RecurringTransactionDocument>();
        if (document.UserId != userId.Value)
        {
            return null;
        }

        return RecurringTransactionDocumentMapper.ToDomain(recurringTransactionId, document);
    }

    public async Task SaveAsync(RecurringTransaction recurringTransaction, CancellationToken cancellationToken = default)
    {
        var document = RecurringTransactionDocumentMapper.ToDocument(recurringTransaction);

        await _firestoreDb.Collection(CollectionName)
            .Document(recurringTransaction.Id.Value)
            .SetAsync(document, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(UserId userId, RecurringTransactionId recurringTransactionId, CancellationToken cancellationToken = default)
    {
        await _firestoreDb.Collection(CollectionName)
            .Document(recurringTransactionId.Value)
            .DeleteAsync(cancellationToken: cancellationToken);
    }
}
