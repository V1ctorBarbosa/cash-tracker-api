using System.Security.Claims;
using CashTrack.Api.Authentication;
using CashTrack.Application.Transactions;
using CashTrack.Domain.Transactions;

namespace CashTrack.Api.Endpoints;

public static class TransactionEndpoints
{
    private const string RoutePrefix = "/transactions";

    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).RequireAuthorization();

        group.MapGet("/", ListTransactionsAsync).WithName(RouteNames.ListTransactions);
        group.MapGet("/{id}", GetTransactionAsync).WithName(RouteNames.GetTransaction);
        group.MapPost("/", CreateTransactionAsync).WithName(RouteNames.CreateTransaction);
        group.MapPut("/{id}", UpdateTransactionAsync).WithName(RouteNames.UpdateTransaction);
        group.MapDelete("/{id}", DeleteTransactionAsync).WithName(RouteNames.DeleteTransaction);
        group.MapDelete("/", DeleteTransactionsInRangeAsync).WithName(RouteNames.DeleteTransactionsInRange);

        return app;
    }

    private static async Task<IResult> ListTransactionsAsync(
        ClaimsPrincipal user,
        TransactionService transactionService,
        CancellationToken cancellationToken,
        DateOnly? from = null,
        DateOnly? to = null,
        string? tagId = null,
        string? type = null,
        string? paymentMethod = null)
    {
        var userId = user.GetAuthenticatedUserId();
        var transactions = await transactionService.ListAsync(userId, from, to, tagId, type, paymentMethod, cancellationToken);

        return Results.Ok(transactions);
    }

    private static async Task<IResult> GetTransactionAsync(
        ClaimsPrincipal user,
        string id,
        TransactionService transactionService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var transaction = await transactionService.GetAsync(userId, new TransactionId(id), cancellationToken);

        if (transaction is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(transaction);
    }

    private static async Task<IResult> CreateTransactionAsync(
        ClaimsPrincipal user,
        CreateTransactionRequest request,
        TransactionService transactionService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var transaction = await transactionService.CreateAsync(userId, request, cancellationToken);

        return Results.Ok(transaction);
    }

    private static async Task<IResult> UpdateTransactionAsync(
        ClaimsPrincipal user,
        string id,
        UpdateTransactionRequest request,
        TransactionService transactionService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var transaction = await transactionService.UpdateAsync(userId, new TransactionId(id), request, cancellationToken);

        if (transaction is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(transaction);
    }

    private static async Task<IResult> DeleteTransactionAsync(
        ClaimsPrincipal user,
        string id,
        TransactionService transactionService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var wasDeleted = await transactionService.DeleteAsync(userId, new TransactionId(id), cancellationToken);

        if (!wasDeleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Both bounds are required (non-nullable parameters bind to a 400 when missing), so a bare
    /// DELETE /transactions can never wipe a user's whole history.
    /// </summary>
    private static async Task<IResult> DeleteTransactionsInRangeAsync(
        ClaimsPrincipal user,
        DateOnly from,
        DateOnly to,
        TransactionService transactionService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var deletedTransactions = await transactionService.DeleteInRangeAsync(userId, from, to, cancellationToken);

        return Results.Ok(deletedTransactions);
    }

    private static class RouteNames
    {
        public const string ListTransactions = "ListTransactions";
        public const string GetTransaction = "GetTransaction";
        public const string CreateTransaction = "CreateTransaction";
        public const string UpdateTransaction = "UpdateTransaction";
        public const string DeleteTransaction = "DeleteTransaction";
        public const string DeleteTransactionsInRange = "DeleteTransactionsInRange";
    }
}
