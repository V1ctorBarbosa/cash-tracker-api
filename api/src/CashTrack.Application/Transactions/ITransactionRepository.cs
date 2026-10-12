using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Transactions;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> ListByUserAsync(UserId userId, TransactionQuery query, CancellationToken cancellationToken = default);

    /// <summary>How many transactions the user has in total, or only with the given tag when <paramref name="tagId"/> is set.</summary>
    Task<int> CountByUserAsync(UserId userId, TagId? tagId, CancellationToken cancellationToken = default);

    /// <summary>The date of the user's oldest transaction, or null when they have none.</summary>
    Task<DateOnly?> FindEarliestDateAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Which (recurring definition, month) pairs in [<paramref name="fromMonth"/>, <paramref name="toMonth"/>]
    /// already have a transaction (Section 12's generation guard), answered for every given
    /// definition at once so materialization costs a fixed number of round trips, not one per
    /// recurrence per month.
    /// </summary>
    Task<IReadOnlySet<RecurringOccurrence>> ListMaterializedOccurrencesAsync(
        UserId userId,
        IReadOnlyCollection<RecurringTransactionId> recurringTransactionIds,
        YearMonth fromMonth,
        YearMonth toMonth,
        CancellationToken cancellationToken = default);

    Task SaveAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task SaveManyAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default);

    Task DeleteManyAsync(UserId userId, IReadOnlyCollection<TransactionId> transactionIds, CancellationToken cancellationToken = default);
}
