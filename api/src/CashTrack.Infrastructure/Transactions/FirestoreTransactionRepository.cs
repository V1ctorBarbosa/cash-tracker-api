using System.Globalization;
using CashTrack.Application.Transactions;
using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using Google.Cloud.Firestore;
// Google.Cloud.Firestore also defines a Transaction type (its own transaction/batched-write
// primitive) -- alias ours explicitly so every unqualified `Transaction` below means the
// domain entity.
using Transaction = CashTrack.Domain.Transactions.Transaction;

namespace CashTrack.Infrastructure.Transactions;

public sealed class FirestoreTransactionRepository : ITransactionRepository
{
    private const string CollectionName = "transactions";
    private const string UserIdFieldName = "UserId";
    private const string DateFieldName = "Date";
    private const string TagIdFieldName = "TagId";
    private const string TypeFieldName = "Type";
    private const string PaymentMethodFieldName = "PaymentMethod";
    private const string RecurringTransactionIdFieldName = "RecurringTransactionId";
    private const string DateFormat = "yyyy-MM-dd";

    // Firestore rejects a single batched write with more than 500 operations.
    private const int MaxWritesPerBatch = 500;

    private readonly FirestoreDb _firestoreDb;

    public FirestoreTransactionRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<IReadOnlyList<Transaction>> ListByUserAsync(
        UserId userId,
        TransactionQuery query,
        CancellationToken cancellationToken = default)
    {
        Query firestoreQuery = _firestoreDb.Collection(CollectionName).WhereEqualTo(UserIdFieldName, userId.Value);

        if (query.From is { } from)
        {
            firestoreQuery = firestoreQuery.WhereGreaterThanOrEqualTo(DateFieldName, from.ToString(DateFormat, CultureInfo.InvariantCulture));
        }

        if (query.To is { } to)
        {
            firestoreQuery = firestoreQuery.WhereLessThanOrEqualTo(DateFieldName, to.ToString(DateFormat, CultureInfo.InvariantCulture));
        }

        if (query.TagId is { } tagId)
        {
            firestoreQuery = firestoreQuery.WhereEqualTo(TagIdFieldName, tagId.Value);
        }

        if (query.Type is { } type)
        {
            firestoreQuery = firestoreQuery.WhereEqualTo(TypeFieldName, type.ToString());
        }

        if (query.PaymentMethod is { } paymentMethod)
        {
            firestoreQuery = firestoreQuery.WhereEqualTo(PaymentMethodFieldName, paymentMethod.ToString());
        }

        var querySnapshot = await firestoreQuery.GetSnapshotAsync(cancellationToken);

        return querySnapshot.Documents
            .Select(document => TransactionDocumentMapper.ToDomain(new TransactionId(document.Id), document.ConvertTo<TransactionDocument>()))
            .ToList();
    }

    public async Task<Transaction?> GetByIdAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _firestoreDb.Collection(CollectionName)
            .Document(transactionId.Value)
            .GetSnapshotAsync(cancellationToken);

        if (!snapshot.Exists)
        {
            return null;
        }

        var document = snapshot.ConvertTo<TransactionDocument>();
        if (document.UserId != userId.Value)
        {
            return null;
        }

        return TransactionDocumentMapper.ToDomain(transactionId, document);
    }

    public async Task<bool> ExistsForRecurringTransactionAsync(
        UserId userId,
        RecurringTransactionId recurringTransactionId,
        YearMonth month,
        CancellationToken cancellationToken = default)
    {
        var monthStart = new DateOnly(month.Year, month.Month, 1);
        var monthEnd = new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));

        var querySnapshot = await _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(UserIdFieldName, userId.Value)
            .WhereEqualTo(RecurringTransactionIdFieldName, recurringTransactionId.Value)
            .WhereGreaterThanOrEqualTo(DateFieldName, monthStart.ToString(DateFormat, CultureInfo.InvariantCulture))
            .WhereLessThanOrEqualTo(DateFieldName, monthEnd.ToString(DateFormat, CultureInfo.InvariantCulture))
            .Limit(1)
            .GetSnapshotAsync(cancellationToken);

        return querySnapshot.Count > 0;
    }

    public async Task SaveAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        var document = TransactionDocumentMapper.ToDocument(transaction);

        await _firestoreDb.Collection(CollectionName)
            .Document(transaction.Id.Value)
            .SetAsync(document, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default)
    {
        await _firestoreDb.Collection(CollectionName)
            .Document(transactionId.Value)
            .DeleteAsync(cancellationToken: cancellationToken);
    }

    public async Task DeleteManyAsync(UserId userId, IReadOnlyCollection<TransactionId> transactionIds, CancellationToken cancellationToken = default)
    {
        foreach (var transactionIdsChunk in transactionIds.Chunk(MaxWritesPerBatch))
        {
            var batch = _firestoreDb.StartBatch();
            foreach (var transactionId in transactionIdsChunk)
            {
                batch.Delete(_firestoreDb.Collection(CollectionName).Document(transactionId.Value));
            }

            await batch.CommitAsync(cancellationToken);
        }
    }
}
