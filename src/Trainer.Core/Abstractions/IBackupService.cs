namespace Trainer.Core.Abstractions;

public interface IBackupService
{
    Task<string> ExportAsync(CancellationToken ct = default);
    Task ImportAsync(string filePath, CancellationToken ct = default);
}
