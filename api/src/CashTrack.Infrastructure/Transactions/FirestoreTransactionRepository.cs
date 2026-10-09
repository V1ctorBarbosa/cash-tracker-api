using System.Globalization;
using CashTrack.Application.Transactions;
using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Tags;
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

    // Firestore rejects a WhereIn filter with more than 30 values.
    private const int MaxWhereInValues = 30;

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

    /// <summary>A count aggregation, billed at one read per thousand matching index entries rather than one per transaction.</summary>
    public async Task<int> CountByUserAsync(UserId userId, TagId? tagId, CancellationToken cancellationToken = default)
    {
        Query firestoreQuery = _firestoreDb.Collection(CollectionName).WhereEqualTo(UserIdFieldName, userId.Value);

        if (tagId is { } givenTagId)
        {
            firestoreQuery = firestoreQuery.WhereEqualTo(TagIdFieldName, givenTagId.Value);
        }

        var aggregateSnapshot = await firestoreQuery.Count().GetSnapshotAsync(cancellationToken);
        return checked((int)(aggregateSnapshot.Count ?? 0));
    }

    public async Task<DateOnly?> FindEarliestDateAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var querySnapshot = await _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(UserIdFieldName, userId.Value)
            .OrderBy(DateFieldName)
            .Limit(1)
            .GetSnapshotAsync(cancellationToken);

        if (querySnapshot.Count == 0)
        {
            return null;
        }

        return TransactionDocumentMapper.ParseDate(querySnapshot.Documents[0].ConvertTo<TransactionDocument>().Date);
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

    public async Task<IReadOnlySet<RecurringOccurrence>> ListMaterializedOccurrencesAsync(
        UserId userId,
        IReadOnlyCollection<RecurringTransactionId> recurringTransactionIds,
        YearMonth fromMonth,
        YearMonth toMonth,
        CancellationToken cancellationToken = default)
    {
        var chunkSnapshots = await Task.WhenAll(recurringTransactionIds
            .Chunk(MaxWhereInValues)
            .Select(recurringTransactionIdsChunk => QueryRecurringTransactionsInRangeAsync(
                userId,
                recurringTransactionIdsChunk,
                fromMonth,
                toMonth,
                cancellationToken)));

        return chunkSnapshots
            .SelectMany(querySnapshot => querySnapshot.Documents)
            .Select(document => TransactionDocumentMapper.ToRecurringOccurrence(document.ConvertTo<TransactionDocument>()))
            .OfType<RecurringOccurrence>()
            .ToHashSet();
    }

    public async Task SaveAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        var document = TransactionDocumentMapper.ToDocument(transaction);

        await _firestoreDb.Collection(CollectionName)
            .Document(transaction.Id.Value)
            .SetAsync(document, cancellationToken: cancellationToken);
    }

    public async Task SaveManyAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        foreach (var transactionsChunk in transactions.Chunk(MaxWritesPerBatch))
        {
            var batch = _firestoreDb.StartBatch();
            foreach (var transaction in transactionsChunk)
            {
                batch.Set(_firestoreDb.Collection(CollectionName).Document(transaction.Id.Value), TransactionDocumentMapper.ToDocument(transaction));
            }

            await batch.CommitAsync(cancellationToken);
        }
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

    private Task<QuerySnapshot> QueryRecurringTransactionsInRangeAsync(
        UserId userId,
        IEnumerable<RecurringTransactionId> recurringTransactionIds,
        YearMonth fromMonth,
        YearMonth toMonth,
        CancellationToken cancellationToken) =>
        _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(UserIdFieldName, userId.Value)
            .WhereIn(RecurringTransactionIdFieldName, recurringTransactionIds.Select(recurringTransactionId => recurringTransactionId.Value))
            .WhereGreaterThanOrEqualTo(DateFieldName, fromMonth.FirstDay().ToString(DateFormat, CultureInfo.InvariantCulture))
            .WhereLessThanOrEqualTo(DateFieldName, toMonth.LastDay().ToString(DateFormat, CultureInfo.InvariantCulture))
            .GetSnapshotAsync(cancellationToken);
}
