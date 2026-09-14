using System.Security.Claims;
using CashTrack.Api.Authentication;
using CashTrack.Application.Tags;
using CashTrack.Domain.Tags;

namespace CashTrack.Api.Endpoints;

public static class TagEndpoints
{
    private const string RoutePrefix = "/tags";

    public static IEndpointRouteBuilder MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).RequireAuthorization();

        group.MapGet("/", ListTagsAsync).WithName(RouteNames.ListTags);
        group.MapPost("/", CreateTagAsync).WithName(RouteNames.CreateTag);
        group.MapPut("/{id}", UpdateTagAsync).WithName(RouteNames.UpdateTag);
        group.MapDelete("/{id}", DeleteTagAsync).WithName(RouteNames.DeleteTag);

        return app;
    }

    private static async Task<IResult> ListTagsAsync(
        ClaimsPrincipal user,
        TagService tagService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var tags = await tagService.ListAsync(userId, cancellationToken);

        return Results.Ok(tags);
    }

    private static async Task<IResult> CreateTagAsync(
        ClaimsPrincipal user,
        CreateTagRequest request,
        TagService tagService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var tag = await tagService.CreateAsync(userId, request, cancellationToken);

        return Results.Ok(tag);
    }

    private static async Task<IResult> UpdateTagAsync(
        ClaimsPrincipal user,
        string id,
        UpdateTagRequest request,
        TagService tagService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var tag = await tagService.UpdateAsync(userId, new TagId(id), request, cancellationToken);

        if (tag is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(tag);
    }

    private static async Task<IResult> DeleteTagAsync(
        ClaimsPrincipal user,
        string id,
        TagService tagService,
        CancellationToken cancellationToken)
    {
        var userId = user.GetAuthenticatedUserId();
        var wasDeleted = await tagService.DeleteAsync(userId, new TagId(id), cancellationToken);

        if (!wasDeleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    private static class RouteNames
    {
        public const string ListTags = "ListTags";
        public const string CreateTag = "CreateTag";
        public const string UpdateTag = "UpdateTag";
        public const string DeleteTag = "DeleteTag";
    }
}
