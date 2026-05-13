using Microsoft.EntityFrameworkCore;
using Trainer.Api;
using Trainer.Api.Tenancy;
using Trainer.Core.Abstractions;
using Trainer.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured. " +
        "Run: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<your Neon connection string>\"");

builder.Services.AddTrainerData(opt => opt.UseNpgsql(connectionString));

// Этап 1b: tenant пока берётся из HTTP-заголовка X-Tenant-Id (для теста через Swagger).
// На этапе 2 заменим на чтение из JWT-claim.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpHeaderTenantContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Trainer.Api is up");

app.MapClientsEndpoints();

app.Run();
