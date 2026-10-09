using System.Security.Claims;
using CashTrack.Api.Authentication;
using CashTrack.Application.Dashboard;

namespace CashTrack.Api.Endpoints;

public static class DashboardEndpoints
{
    private const string RoutePrefix = "/dashboard";

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup(RoutePrefix)
            .RequireAuthorization()
            .MapGet("/", GetDashboardAsync)
            .WithName(RouteNames.GetDashboard);

        return app;
    }

    private static async Task<IResult> GetDashboardAsync(
        ClaimsPrincipal user,
        DateOnly from,
        DateOnly to,
        DashboardService dashboardService,
        CancellationToken cancellationToken,
        DateOnly? trendFrom = null)
    {
        var userId = user.GetAuthenticatedUserId();
        var dashboard = await dashboardService.GetAsync(userId, from, to, trendFrom, cancellationToken);

        return Results.Ok(dashboard);
    }

    private static class RouteNames
    {
        public const string GetDashboard = "GetDashboard";
    }
}
