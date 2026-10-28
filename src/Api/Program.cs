using Api.Data;
using Api.Features.Health;
using Api.Features.Items;
using Api.Platform;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// The platform's settings, such as DATABASE_URL, become options here, and only here.
builder.Configuration.AddPlatformSettings();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddAppDbContext(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>(tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Every route is under /api, the path the front end's nginx passes on unchanged.
var api = app.MapGroup("/api");
api.MapOpenApi("/openapi/{documentName}.json");
api.MapHealthInfo();
api.MapItems();

// Liveness: the process answers. Readiness: Postgres answers too, so a revision
// gets traffic only once it can serve it.
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/readyz", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Logger.LogInformation("Starting in the environment {Environment}", app.Configuration["Platform:Environment"]);

await app.RunAsync();
