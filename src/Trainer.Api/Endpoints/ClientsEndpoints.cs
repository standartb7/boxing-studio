using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Api;

public static class ClientsEndpoints
{
    public static IEndpointRouteBuilder MapClientsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients").WithTags("Clients").RequireAuthorization();

        group.MapGet("/", async (IClientService clients, CancellationToken ct) =>
        {
            var list = await clients.GetActiveAsync(ct);
            return Results.Ok(list);
        });

        group.MapGet("/{id:guid}", async (Guid id, IClientService clients, CancellationToken ct) =>
        {
            var client = await clients.GetByIdAsync(id, ct);
            return client is null ? Results.NotFound() : Results.Ok(client);
        });

        group.MapPost("/", async (Client client, IClientService clients, CancellationToken ct) =>
        {
            var created = await clients.CreateAsync(client, ct);
            return Results.Created($"/api/clients/{created.Id}", created);
        });

        return app;
    }
}
