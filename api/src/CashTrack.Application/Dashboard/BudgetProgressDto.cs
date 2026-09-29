namespace CashTrack.Application.Dashboard;

public sealed record BudgetProgressDto(string TagId, string TagName, string TagColor, string Month, decimal LimitAmount, decimal ActualAmount);
