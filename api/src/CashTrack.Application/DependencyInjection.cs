using CashTrack.Application.Budgets;
using CashTrack.Application.Dashboard;
using CashTrack.Application.RecurringTransactions;
using CashTrack.Application.Tags;
using CashTrack.Application.Transactions;
using CashTrack.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace CashTrack.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<UserProfileService>();
        services.AddScoped<TagService>();
        services.AddScoped<TransactionService>();
        services.AddScoped<RecurringTransactionService>();
        services.AddScoped<RecurringTransactionMaterializationService>();
        services.AddScoped<BudgetService>();
        services.AddScoped<DashboardService>();

        return services;
    }
}
