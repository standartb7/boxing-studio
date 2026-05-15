namespace Trainer.Core.Abstractions;

/// <summary>
/// Экспорт/импорт всех пользовательских данных через JSON. Используется для бэкапа
/// и переноса между устройствами. Импорт — replace-all: стирает текущую БД и заливает новую.
/// </summary>
public interface IBackupService
{
    Task<string> ExportJsonAsync(CancellationToken ct = default);
    Task ImportJsonAsync(string json, CancellationToken ct = default);
}
