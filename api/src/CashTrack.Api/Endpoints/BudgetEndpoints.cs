using System.Security.Claims;
using CashTrack.Api.Authentication;
using CashTrack.Application.Budgets;

namespace CashTrack.Api.Endpoints;

public static class BudgetEndpoints
{
    private const string RoutePrefix = "/budgets";

    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).RequireAuthorization();

        group.MapGet("/", ListBudgetsAsync).WithName(RouteNames.ListBudgets);
        group.MapPut("/{tagId}/{month}", UpsertBudgetAsync).WithName(RouteNames.UpsertBudget);
        group.MapDelete("/{tagId}/{month}", DeleteBudgetAsync).WithName(RouteNames.DeleteBudget);

        return app;
    }

    private static async Task<IResult> ListBudgetsAsync(
        ClaimsPrincipal user,
        BudgetService budgetService,
        CancellationToken cancellationToken,
        string? month = null)
    {
        var userId = user.GetAuthenticatedUserId();
        var budgets = await budgetService.ListAsync(userId, month, cancellationToken);

        return Results.Ok(budgets);
    }

    private static async Task<IResult> UpsertBudgetAsync(
        ClaimsPrincipal user,
        string tagId,
        string month,
        UpsertBudgetRequest request,
        BudgetService budgetService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var budget = await budgetService.UpsertAsync(userId, tagId, month, request, cancellationToken);

        return Results.Ok(budget);
    }

    private static async Task<IResult> DeleteBudgetAsync(
        ClaimsPrincipal user,
        string tagId,
        string month,
        BudgetService budgetService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var wasDeleted = await budgetService.DeleteAsync(userId, tagId, month, cancellationToken);

        if (!wasDeleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    private static class RouteNames
    {
        public const string ListBudgets = "ListBudgets";
        public const string UpsertBudget = "UpsertBudget";
        public const string DeleteBudget = "DeleteBudget";
    }
}
