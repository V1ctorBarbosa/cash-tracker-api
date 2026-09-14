namespace CashTrack.Application.Dashboard;

public sealed record DashboardDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    IReadOnlyList<MonthlySummaryDto> MonthlySummary,
    IReadOnlyList<TagAmountDto> SpendByTag,
    IReadOnlyList<PaymentMethodAmountDto> SpendByPaymentMethod,
    IReadOnlyList<BudgetProgressDto> Budgets);
