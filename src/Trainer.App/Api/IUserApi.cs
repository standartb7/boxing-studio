using Refit;
using Trainer.Contracts;

namespace Trainer.App.Api;

public interface IUserApi
{
    [Get("/api/users")]
    Task<List<UserDto>> GetAllAsync(CancellationToken ct = default);

    [Get("/api/users/{id}")]
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    [Put("/api/users/{id}")]
    Task<UserDto> UpdateAsync(Guid id, [Body] UpdateUserRequest req, CancellationToken ct = default);

    [Delete("/api/users/{id}")]
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}
