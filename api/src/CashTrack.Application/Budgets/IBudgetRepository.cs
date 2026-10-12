using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Budgets;

public interface IBudgetRepository
{
    Task<IReadOnlyList<Budget>> ListByUserAsync(UserId userId, YearMonth? month, CancellationToken cancellationToken = default);

    /// <summary>Every budget whose month falls in [<paramref name="fromMonth"/>, <paramref name="toMonth"/>], inclusive on both ends.</summary>
    Task<IReadOnlyList<Budget>> ListByUserBetweenMonthsAsync(
        UserId userId,
        YearMonth fromMonth,
        YearMonth toMonth,
        CancellationToken cancellationToken = default);

    Task<Budget?> GetByIdAsync(UserId userId, BudgetId budgetId, CancellationToken cancellationToken = default);

    Task SaveAsync(Budget budget, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserId userId, BudgetId budgetId, CancellationToken cancellationToken = default);
}
