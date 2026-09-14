using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Users;

namespace CashTrack.Application.RecurringTransactions;

public interface IRecurringTransactionRepository
{
    Task<IReadOnlyList<RecurringTransaction>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<RecurringTransaction?> GetByIdAsync(UserId userId, RecurringTransactionId recurringTransactionId, CancellationToken cancellationToken = default);

    Task SaveAsync(RecurringTransaction recurringTransaction, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserId userId, RecurringTransactionId recurringTransactionId, CancellationToken cancellationToken = default);
}
