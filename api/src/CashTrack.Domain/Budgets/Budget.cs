using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;

namespace CashTrack.Domain.Budgets;

/// <summary>
/// A monthly spending goal for a tag (Section 13 of the implementation document). Nothing here
/// restricts it to expense tags -- the dashboard is what only ever compares it against expense
/// totals, since a goal only makes sense as a spending ceiling.
/// </summary>
public sealed class Budget
{
    public BudgetId Id { get; }

    public UserId UserId { get; }

    public TagId TagId { get; }

    public YearMonth Month { get; }

    public Money LimitAmount { get; }

    private Budget(BudgetId id, UserId userId, TagId tagId, YearMonth month, Money limitAmount)
    {
        Id = id;
        UserId = userId;
        TagId = tagId;
        Month = month;
        LimitAmount = limitAmount;
    }

    public static Budget Create(UserId userId, TagId tagId, YearMonth month, Money limitAmount) =>
        new(BudgetId.For(userId, tagId, month), userId, tagId, month, limitAmount);
}
