using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Budgets;

public sealed class BudgetService
{
    private readonly IBudgetRepository _budgetRepository;

    public BudgetService(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<IReadOnlyList<BudgetDto>> ListAsync(UserId userId, string? month, CancellationToken cancellationToken = default)
    {
        var budgets = await _budgetRepository.ListByUserAsync(userId, month is null ? null : YearMonth.Parse(month), cancellationToken);
        return budgets.Select(ToDto).ToList();
    }

    /// <summary>Upserts the budget for (userId, tagId, month) -- Budget's deterministic id already makes this a plain overwrite (Section 13).</summary>
    public async Task<BudgetDto> UpsertAsync(
        UserId userId,
        string tagId,
        string month,
        UpsertBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        var budget = Budget.Create(userId, new TagId(tagId), YearMonth.Parse(month), new Money(request.LimitAmount));

        await _budgetRepository.SaveAsync(budget, cancellationToken);
        return ToDto(budget);
    }

    public async Task<bool> DeleteAsync(UserId userId, string tagId, string month, CancellationToken cancellationToken = default)
    {
        var budgetId = BudgetId.For(userId, new TagId(tagId), YearMonth.Parse(month));
        var existingBudget = await _budgetRepository.GetByIdAsync(userId, budgetId, cancellationToken);
        if (existingBudget is null)
        {
            return false;
        }

        await _budgetRepository.DeleteAsync(userId, budgetId, cancellationToken);
        return true;
    }

    /// <summary>Deletes every budget set for this tag, across every month -- called when the tag itself is deleted, so no budget is left pointing at a tag that no longer exists.</summary>
    public async Task DeleteAllForTagAsync(UserId userId, TagId tagId, CancellationToken cancellationToken = default)
    {
        var budgets = await _budgetRepository.ListByUserAsync(userId, month: null, cancellationToken);
        var budgetsForTag = budgets.Where(budget => budget.TagId == tagId);

        foreach (var budget in budgetsForTag)
        {
            await _budgetRepository.DeleteAsync(userId, budget.Id, cancellationToken);
        }
    }

    private static BudgetDto ToDto(Budget budget) => new(budget.Id.Value, budget.TagId.Value, budget.Month.ToString(), budget.LimitAmount.Amount);
}
