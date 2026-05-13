using System.Net.Http.Json;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.Services.Api;

public class ApiClientService : IClientService
{
    private readonly HttpClient _http;

    public ApiClientService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("api");
    }

    public async Task<IReadOnlyList<Client>> GetActiveAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<Client>>("/api/clients", ct);
        return list ?? new List<Client>();
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/clients/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Client>(cancellationToken: ct);
    }

    public async Task<Client> CreateAsync(Client client, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/clients", client, ct);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Client>(cancellationToken: ct);
        return created ?? client;
    }

    public Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        // TODO: добавить PUT /api/clients/{id} endpoint и реализовать
        throw new NotImplementedException("Update endpoint not yet implemented on the server");
    }

    public Task ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        // TODO: добавить DELETE /api/clients/{id} endpoint и реализовать
        throw new NotImplementedException("Archive endpoint not yet implemented on the server");
    }
}
