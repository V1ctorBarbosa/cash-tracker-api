using CashTrack.Application.Budgets;
using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Users;
using Google.Cloud.Firestore;

namespace CashTrack.Infrastructure.Budgets;

public sealed class FirestoreBudgetRepository : IBudgetRepository
{
    private const string CollectionName = "budgets";
    private const string UserIdFieldName = "UserId";
    private const string MonthFieldName = "Month";

    private readonly FirestoreDb _firestoreDb;

    public FirestoreBudgetRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<IReadOnlyList<Budget>> ListByUserAsync(UserId userId, YearMonth? month, CancellationToken cancellationToken = default)
    {
        Query query = _firestoreDb.Collection(CollectionName).WhereEqualTo(UserIdFieldName, userId.Value);

        if (month is { } givenMonth)
        {
            query = query.WhereEqualTo(MonthFieldName, givenMonth.ToString());
        }

        var querySnapshot = await query.GetSnapshotAsync(cancellationToken);

        return querySnapshot.Documents
            .Select(document => BudgetDocumentMapper.ToDomain(document.ConvertTo<BudgetDocument>()))
            .ToList();
    }

    public async Task<Budget?> GetByIdAsync(UserId userId, BudgetId budgetId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _firestoreDb.Collection(CollectionName)
            .Document(budgetId.Value)
            .GetSnapshotAsync(cancellationToken);

        if (!snapshot.Exists)
        {
            return null;
        }

        var document = snapshot.ConvertTo<BudgetDocument>();
        if (document.UserId != userId.Value)
        {
            return null;
        }

        return BudgetDocumentMapper.ToDomain(document);
    }

    public async Task SaveAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        var document = BudgetDocumentMapper.ToDocument(budget);

        await _firestoreDb.Collection(CollectionName)
            .Document(budget.Id.Value)
            .SetAsync(document, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(UserId userId, BudgetId budgetId, CancellationToken cancellationToken = default)
    {
        await _firestoreDb.Collection(CollectionName)
            .Document(budgetId.Value)
            .DeleteAsync(cancellationToken: cancellationToken);
    }
}
