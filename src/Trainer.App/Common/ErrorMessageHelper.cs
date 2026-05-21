using System.Net.Http;
using Refit;

namespace Trainer.App.Common;

/// <summary>
/// Turns wire-level exceptions into messages that make sense to a non-technical trainer.
/// Used by list pages to render an inline banner instead of dumping stack traces in
/// modal alerts.
/// </summary>
public static class ErrorMessageHelper
{
    public static string Format(Exception ex)
    {
        switch (ex)
        {
            case TaskCanceledException:
            case HttpRequestException:
                return "Нет связи с сервером. Проверь интернет и попробуй ещё раз.";

            case ApiException api:
                if ((int)api.StatusCode == 401)
                    return "Сессия истекла. Войди заново.";
                if ((int)api.StatusCode >= 500)
                    return "Сервер временно недоступен. Попробуй через минуту.";
                // Server returns { error: "..." } on 4xx — try to surface it; fall back to status code.
                var body = api.Content?.Trim();
                if (!string.IsNullOrEmpty(body))
                {
                    // Quick parse: { "error": "..." } — strip the JSON shell.
                    var maybeMessage = TryExtractErrorField(body);
                    if (!string.IsNullOrEmpty(maybeMessage)) return maybeMessage!;
                    if (body.Length < 200) return body;
                }
                return $"Не получилось (HTTP {(int)api.StatusCode}).";

            default:
                return ex.Message;
        }
    }

    private static string? TryExtractErrorField(string body)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                && doc.RootElement.TryGetProperty("error", out var err)
                && err.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return err.GetString();
            }
        }
        catch
        {
        }
        return null;
    }
}
