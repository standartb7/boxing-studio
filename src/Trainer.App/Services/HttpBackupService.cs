using System.Net.Http.Json;
using System.Text;
using Trainer.Core.Abstractions;

namespace Trainer.App.Services;

/// <summary>
/// Cloud backup: pulls/pushes the gym-wide JSON via the API. The wire format is whatever
/// the server's BackupController emits — we treat it as opaque text on the mobile side,
/// because only HeadTrainer can ever call these endpoints anyway (server enforces the policy).
/// </summary>
public class HttpBackupService : IBackupService
{
    public const string HttpClientName = "trainer-api";

    private readonly IHttpClientFactory _factory;

    public HttpBackupService(IHttpClientFactory factory) => _factory = factory;

    public async Task<string> ExportJsonAsync(CancellationToken ct = default)
    {
        var http = _factory.CreateClient(HttpClientName);
        using var resp = await http.GetAsync("/api/backup/export", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }

    public async Task ImportJsonAsync(string json, CancellationToken ct = default)
    {
        var http = _factory.CreateClient(HttpClientName);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await http.PostAsync("/api/backup/import", content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body)
                    ? $"Import failed: HTTP {(int)resp.StatusCode}"
                    : body);
        }
    }
}
