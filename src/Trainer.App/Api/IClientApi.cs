using Refit;
using Trainer.Contracts;

namespace Trainer.App.Api;

public interface IClientApi
{
    [Get("/api/clients")]
    Task<List<ClientDto>> GetAllAsync([Query] Guid? ownerTrainerId = null, CancellationToken ct = default);

    [Get("/api/clients/{id}")]
    Task<ClientDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    [Post("/api/clients")]
    Task<ClientDto> CreateAsync([Body] CreateClientRequest req, CancellationToken ct = default);

    [Put("/api/clients/{id}")]
    Task<ClientDto> UpdateAsync(Guid id, [Body] UpdateClientRequest req, CancellationToken ct = default);

    [Delete("/api/clients/{id}")]
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    [Post("/api/clients/{id}/reassign")]
    Task<ClientDto> ReassignAsync(Guid id, [Body] ReassignClientRequest req, CancellationToken ct = default);
}
