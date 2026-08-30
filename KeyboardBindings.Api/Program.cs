using KeyboardBindings.Api.Contracts;
using KeyboardBindings.Api.Data;
using KeyboardBindings.Api.Http;
using KeyboardBindings.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Payloads are small; cap the body size to shrink the DoS surface (default ~30 MB).
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);

builder.Services.AddOpenApi();

// Standardize error responses as RFC 7807 problem+json.
builder.Services.AddProblemDetails();

// Validate minimal-API parameters via their DataAnnotations before the handler runs, so malformed requests
// (e.g. a null/missing 'mappings' array or a null element) are rejected with a 400 at the boundary.
builder.Services.AddValidation();

// NOTE: In a real deployment this would be supplied by an environment variable (or a secret
// store), not committed to source. It lives in appsettings.json to keep local setup frictionless.
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
           .AddInterceptors(new SqlitePragmaInterceptor()));
builder.Services.AddScoped<MappingService>();

// Liveness/readiness: verifies the app can reach the SQLite database.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Apply pending migrations on startup (they also carry the seed data) so the app is runnable out of the box.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();

app.MapHealthChecks("/health");

// Get all current key mappings for a keyboard (remapped or not).
app.MapGet("/keyboards/{name}/mappings", async (string name, MappingService service, CancellationToken ct) =>
{
    var result = await service.GetMappingsAsync(name, ct);
    return result.Status switch
    {
        MappingStatus.Success => Results.Ok(result.Response),
        MappingStatus.KeyboardNotFound => KeyboardNotFound(name),
        _ => Results.Problem("Unexpected error.")
    };
})
.WithName("GetKeyMappings");

// Assign (validate + save) a set of key mappings for a keyboard.
app.MapPut("/keyboards/{name}/mappings", async (
    string name, AssignMappingsRequest request, MappingService service, HttpResponse response, CancellationToken ct) =>
{
    var result = await service.AssignMappingsAsync(name, request, ct);
    switch (result.Status)
    {
        case MappingStatus.Success:
            return Results.NoContent();
        case MappingStatus.KeyboardNotFound:
            return KeyboardNotFound(name);
        case MappingStatus.ValidationFailed:
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["mappings"] = result.Errors.ToArray() },
                title: "Invalid key mappings");
        case MappingStatus.WriteConflict:
            // Transient contention — tell the client to retry.
            response.Headers.RetryAfter = "1";
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Write conflict",
                detail: result.Errors.FirstOrDefault());
        default:
            return Results.Problem("Unexpected error.");
    }
})
.WithName("AssignKeyMappings");

app.Run();

static IResult KeyboardNotFound(string name) => Results.Problem(
    statusCode: StatusCodes.Status404NotFound,
    title: "Keyboard not found",
    detail: $"Unknown keyboard '{name}'.");

// Exposed so the integration test project can bootstrap the app via WebApplicationFactory<Program>.
public partial class Program;
