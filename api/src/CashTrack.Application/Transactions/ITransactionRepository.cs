using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Transactions;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> ListByUserAsync(UserId userId, TransactionQuery query, CancellationToken cancellationToken = default);

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
