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

        return services;
    }
}
