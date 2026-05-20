using Trainer.App.Api;
using Trainer.Contracts;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.Services;

/// <summary>
/// HTTP-backed implementation of IClientService. Implements the same interface as the
/// legacy SQLite ClientService so ViewModels need no changes when swapped in DI.
/// </summary>
public class HttpClientService : IClientService
{
    private readonly IClientApi _api;
    private readonly ISessionApi _sessions;
    private readonly TrainerFilterContext _filter;

    public HttpClientService(IClientApi api, ISessionApi sessions, TrainerFilterContext filter)
    {
        _api = api;
        _sessions = sessions;
        _filter = filter;
    }

    public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default)
    {
        // Clients are gym-wide — no trainer filter applied.
        var dtos = await _api.GetAllAsync(ct);
        return dtos.Select(d => d.ToEntity()).ToList();
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try { return (await _api.GetByIdAsync(id, ct)).ToEntity(); }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Client> CreateAsync(Client client, CancellationToken ct = default)
    {
        var dto = await _api.CreateAsync(new CreateClientRequest
        {
            Name = client.Name,
            Phone = client.Phone,
            Notes = client.Notes,
        }, ct);
        return dto.ToEntity();
    }

    public async Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        await _api.UpdateAsync(client.Id, new UpdateClientRequest
        {
            Name = client.Name,
            Phone = client.Phone,
            Notes = client.Notes,
            IsActive = client.IsActive,
        }, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try { await _api.DeleteAsync(id, ct); }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Already gone — treat as success.
        }
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetSessionCountsAsync(CancellationToken ct = default)
    {
        // Server doesn't have a dedicated endpoint yet — derive from the sessions list.
        // Cheap enough at expected scale (tens of sessions per gym).
        var sessions = await _sessions.GetAllAsync(ownerTrainerId: _filter.SelectedOwnerTrainerId, ct: ct);
        var counts = new Dictionary<Guid, int>();
        foreach (var s in sessions)
            foreach (var clientId in s.MemberIds)
                counts[clientId] = counts.GetValueOrDefault(clientId) + 1;
        return counts;
    }
}
