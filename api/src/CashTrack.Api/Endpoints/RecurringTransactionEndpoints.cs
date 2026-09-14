using System.Security.Claims;
using CashTrack.Api.Authentication;
using CashTrack.Application.RecurringTransactions;
using CashTrack.Domain.RecurringTransactions;

namespace CashTrack.Api.Endpoints;

public static class RecurringTransactionEndpoints
{
    private const string RoutePrefix = "/recurring-transactions";

    public static IEndpointRouteBuilder MapRecurringTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).RequireAuthorization();

        group.MapGet("/", ListRecurringTransactionsAsync).WithName(RouteNames.ListRecurringTransactions);
        group.MapPost("/", CreateRecurringTransactionAsync).WithName(RouteNames.CreateRecurringTransaction);
        group.MapPut("/{id}", UpdateRecurringTransactionAsync).WithName(RouteNames.UpdateRecurringTransaction);
        group.MapDelete("/{id}", DeleteRecurringTransactionAsync).WithName(RouteNames.DeleteRecurringTransaction);

        return app;
    }

    private static async Task<IResult> ListRecurringTransactionsAsync(
        ClaimsPrincipal user,
        RecurringTransactionService recurringTransactionService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var recurringTransactions = await recurringTransactionService.ListAsync(userId, cancellationToken);

        return Results.Ok(recurringTransactions);
    }

    private static async Task<IResult> CreateRecurringTransactionAsync(
        ClaimsPrincipal user,
        CreateRecurringTransactionRequest request,
        RecurringTransactionService recurringTransactionService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var recurringTransaction = await recurringTransactionService.CreateAsync(userId, request, cancellationToken);

        return Results.Ok(recurringTransaction);
    }

    private static async Task<IResult> UpdateRecurringTransactionAsync(
        ClaimsPrincipal user,
        string id,
        UpdateRecurringTransactionRequest request,
        RecurringTransactionService recurringTransactionService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var recurringTransaction = await recurringTransactionService.UpdateAsync(userId, new RecurringTransactionId(id), request, cancellationToken);

        if (recurringTransaction is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(recurringTransaction);
    }

    private static async Task<IResult> DeleteRecurringTransactionAsync(
        ClaimsPrincipal user,
        string id,
        RecurringTransactionService recurringTransactionService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var wasDeleted = await recurringTransactionService.DeleteAsync(userId, new RecurringTransactionId(id), cancellationToken);

        if (!wasDeleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    private static class RouteNames
    {
        public const string ListRecurringTransactions = "ListRecurringTransactions";
        public const string CreateRecurringTransaction = "CreateRecurringTransaction";
        public const string UpdateRecurringTransaction = "UpdateRecurringTransaction";
        public const string DeleteRecurringTransaction = "DeleteRecurringTransaction";
    }
}
