using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;

namespace CashTrack.Infrastructure.Budgets;

/// <summary>
/// Pure mapping between the domain's <see cref="Budget"/> entity and its Firestore persistence
/// representation. Kept free of any Firestore I/O so it can be unit tested without a live database.
/// </summary>
internal static class BudgetDocumentMapper
{
    public static Budget ToDomain(BudgetDocument document) => Budget.Create(
        new UserId(document.UserId),
        new TagId(document.TagId),
        YearMonth.Parse(document.Month),
        new Money(document.LimitAmount));

    public static BudgetDocument ToDocument(Budget budget) => new()
    {
        UserId = budget.UserId.Value,
        TagId = budget.TagId.Value,
        Month = budget.Month.ToString(),
        LimitAmount = budget.LimitAmount.Amount,
    };
}
