using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Budgets;

public interface IBudgetRepository
{
    Task<IReadOnlyList<Budget>> ListByUserAsync(UserId userId, YearMonth? month, CancellationToken cancellationToken = default);

    Task<Budget?> GetByIdAsync(UserId userId, BudgetId budgetId, CancellationToken cancellationToken = default);

    Task SaveAsync(Budget budget, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserId userId, BudgetId budgetId, CancellationToken cancellationToken = default);
}
