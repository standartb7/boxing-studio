using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Trainer.Api;
using Trainer.Api.Auth;
using Trainer.Data;

// CLI mode: seed-head-trainer <email> <password> <displayName>
if (args.Length > 0 && args[0] == SeedHeadTrainer.Command)
{
    var cfg = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
    return await SeedHeadTrainer.RunAsync(args, cfg.GetConnectionString("Postgres"));
}

var builder = WebApplication.CreateBuilder(args);

// --- Configuration -----------------------------------------------------------
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

// --- Database ----------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");
builder.Services.AddDbContext<TrainerDbContext>(opt =>
    opt.UseNpgsql(connectionString, npg => npg.MigrationsAssembly("Trainer.Api")));

// --- Auth services -----------------------------------------------------------
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITrainerScope, HttpTrainerScope>();

// --- JWT bearer --------------------------------------------------------------
var authSection = builder.Configuration.GetSection(AuthOptions.SectionName);
var authOpts = authSection.Get<AuthOptions>() ?? new AuthOptions();
if (string.IsNullOrWhiteSpace(authOpts.JwtSigningKey))
    throw new InvalidOperationException("Auth:JwtSigningKey is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Don't rewrite "role"/"sub" into long Microsoft URI claim types — we read them
        // by their short names in JwtService and authorization policies.
        options.MapInboundClaims = false;
        var keyBytes = Convert.FromBase64String(authOpts.JwtSigningKey);
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOpts.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = authOpts.JwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = JwtService.ClaimRole,
            NameClaimType = JwtService.ClaimEmail,
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthPolicies.HeadTrainerOnly, p =>
        p.RequireAuthenticatedUser()
         .RequireClaim(JwtService.ClaimRole, nameof(Trainer.Core.Entities.UserRole.HeadTrainer)));

// --- MVC + OpenAPI -----------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

// Apply pending EF migrations on startup. Acceptable on Fly.io single-machine deploys;
// reconsider when scaling out (each instance would race the same migration).
// Skipped in Testing — the WebApplicationFactory swaps in SQLite-in-memory.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TrainerDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
return 0;

// Marker type for WebApplicationFactory<Program> in tests.
public partial class Program;
