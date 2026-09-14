using CashTrack.Application.Tags;
using CashTrack.Application.Transactions;
using CashTrack.Application.Users;
using CashTrack.Infrastructure.Firebase;
using CashTrack.Infrastructure.Tags;
using CashTrack.Infrastructure.Transactions;
using CashTrack.Infrastructure.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CashTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFirebaseAdmin(configuration);
        services.AddScoped<IUserRepository, FirestoreUserRepository>();
        services.AddScoped<ITagRepository, FirestoreTagRepository>();
        services.AddScoped<ITransactionRepository, FirestoreTransactionRepository>();

        return services;
    }
}
