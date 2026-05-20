using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Trainer.Api.Auth;
using Trainer.Data;

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

app.Run();

// Marker type for WebApplicationFactory<Program> in tests.
public partial class Program;
