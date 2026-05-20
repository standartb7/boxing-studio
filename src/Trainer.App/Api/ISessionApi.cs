using Refit;
using Trainer.Contracts;

namespace Trainer.App.Api;

public interface ISessionApi
{
    [Get("/api/sessions")]
    Task<List<SessionDto>> GetAllAsync([Query] Guid? trainingTypeId = null, CancellationToken ct = default);

    [Get("/api/sessions/{id}")]
    Task<SessionDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    [Post("/api/sessions")]
    Task<SessionDto> CreateAsync([Body] CreateSessionRequest req, CancellationToken ct = default);

    [Put("/api/sessions/{id}")]
    Task<SessionDto> UpdateAsync(Guid id, [Body] UpdateSessionRequest req, CancellationToken ct = default);

    [Delete("/api/sessions/{id}")]
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
