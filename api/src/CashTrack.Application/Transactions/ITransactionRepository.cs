using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Transactions;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> ListByUserAsync(UserId userId, TransactionQuery query, CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default);

    /// <summary>Whether a transaction already exists for this recurring definition in the given month (Section 12's generation guard).</summary>
    Task<bool> ExistsForRecurringTransactionAsync(
        UserId userId,
        RecurringTransactionId recurringTransactionId,
        YearMonth month,
        CancellationToken cancellationToken = default);

    Task SaveAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default);
}
