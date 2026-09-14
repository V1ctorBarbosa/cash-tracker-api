namespace CashTrack.Application.Budgets;

public sealed record BudgetDto(string Id, string TagId, string Month, decimal LimitAmount);
