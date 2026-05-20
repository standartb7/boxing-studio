using Refit;
using Trainer.Contracts;

namespace Trainer.App.Api;

public interface ITrainingTypeApi
{
    [Get("/api/training-types")]
    Task<List<TrainingTypeDto>> GetAllAsync(CancellationToken ct = default);

    [Post("/api/training-types")]
    Task<TrainingTypeDto> CreateAsync([Body] CreateTrainingTypeRequest req, CancellationToken ct = default);

    [Put("/api/training-types/{id}")]
    Task<TrainingTypeDto> RenameAsync(Guid id, [Body] RenameTrainingTypeRequest req, CancellationToken ct = default);

    [Delete("/api/training-types/{id}")]
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
