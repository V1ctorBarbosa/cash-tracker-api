using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;

namespace CashTrack.Domain.Budgets;

public readonly record struct BudgetId
{
    public string Value { get; }

    public BudgetId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Budget id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Deterministic from (userId, tagId, month) -- there is one budget per tag per month
    /// (Section 13), so setting a new goal for a combination that already has one must
    /// overwrite it rather than create a duplicate. Deriving the id this way makes that an
    /// ordinary upsert instead of a look-up-then-decide dance.
    /// </summary>
    public static BudgetId For(UserId userId, TagId tagId, YearMonth month) => new($"{userId.Value}_{tagId.Value}_{month}");
}
