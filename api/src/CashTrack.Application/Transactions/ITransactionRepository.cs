using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Transactions;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> ListByUserAsync(UserId userId, TransactionQuery query, CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default);

    Task SaveAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default);
}
