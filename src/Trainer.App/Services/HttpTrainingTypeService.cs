using Trainer.App.Api;
using Trainer.Contracts;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.Services;

public class HttpTrainingTypeService : ITrainingTypeService
{
    private readonly ITrainingTypeApi _api;

    public HttpTrainingTypeService(ITrainingTypeApi api) => _api = api;

    public async Task<IReadOnlyList<TrainingType>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _api.GetAllAsync(ct);
        return list.Select(d => d.ToEntity()).ToList();
    }

    public async Task<TrainingType?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var all = await _api.GetAllAsync(ct);
        return all.FirstOrDefault(t => t.Id == id)?.ToEntity();
    }

    public async Task<TrainingType> CreateOrGetAsync(string name, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Имя группы не может быть пустым", nameof(name));

        try
        {
            var dto = await _api.CreateAsync(new CreateTrainingTypeRequest { Name = trimmed }, ct);
            return dto.ToEntity();
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Already exists — return the existing one (case-insensitive match on server side).
            var all = await _api.GetAllAsync(ct);
            var existing = all.FirstOrDefault(t => string.Equals(t.Name, trimmed, StringComparison.OrdinalIgnoreCase));
            if (existing is null) throw;
            return existing.ToEntity();
        }
    }

    public async Task RenameAsync(Guid id, string newName, CancellationToken ct = default)
    {
        var trimmed = newName.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Имя не может быть пустым", nameof(newName));
        await _api.RenameAsync(id, new RenameTrainingTypeRequest { NewName = trimmed }, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try { await _api.DeleteAsync(id, ct); }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }
    }
}
