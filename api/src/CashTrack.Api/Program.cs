using CashTrack.Api.Authentication;
using CashTrack.Api.Endpoints;
using CashTrack.Api.ErrorHandling;
using CashTrack.Application;
using CashTrack.Infrastructure;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication;

const string DevelopmentCorsPolicyName = "DevelopmentCors";
const string ExpoWebDevServerOrigin = "http://localhost:8081";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevelopmentCorsPolicyName, policy =>
    {
        policy
            .WithOrigins(ExpoWebDevServerOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(FirebaseAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(FirebaseAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(DevelopmentCorsPolicyName);
}

// Skipped in Development: it would force every plain-HTTP request (including the native app's,
// which deliberately calls HTTP directly -- see shared/config/api-config.ts) onto the HTTPS
// endpoint, which serves the ASP.NET Core self-signed dev certificate. Desktop browsers/emulators
// often trust that certificate via `dotnet dev-certs https --trust`, but a real Android device
// never does, so every redirected request fails TLS validation.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("GetHealth");

app.MapGet("/health/firebase", async (FirestoreDb firestoreDb) =>
{
    var snapshot = await firestoreDb.Collection("_health").Document("ping").GetSnapshotAsync();
    return Results.Ok(new
    {
        status = "connected",
        projectId = firestoreDb.ProjectId,
        documentExists = snapshot.Exists,
    });
})
.WithName("GetFirebaseHealth")
.RequireAuthorization();

app.MapProfileEndpoints();
app.MapTagEndpoints();
app.MapTransactionEndpoints();
app.MapRecurringTransactionEndpoints();
app.MapBudgetEndpoints();
app.MapDashboardEndpoints();

app.Run();
