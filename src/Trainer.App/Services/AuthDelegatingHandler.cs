using System.Net;
using System.Net.Http.Headers;

namespace Trainer.App.Services;

/// <summary>
/// Adds the cached access token to every outgoing request. On a 401 we do one refresh
/// attempt and retry — if that fails, the request bubbles up as 401 and the UI shows
/// the login screen. We never refresh more than once per request to avoid loops.
/// </summary>
public class AuthDelegatingHandler : DelegatingHandler
{
    private readonly AuthService _auth;

    public AuthDelegatingHandler(AuthService auth) => _auth = auth;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;

        // 401: try one refresh + retry. RefreshAsync forces a network round-trip.
        response.Dispose();
        var refreshed = await _auth.RefreshAsync(ct);
        if (string.IsNullOrEmpty(refreshed)) return BuildUnauthorized();

        // Re-clone the request — HttpRequestMessage is single-use.
        var retry = await CloneAsync(request);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed);
        return await base.SendAsync(retry, ct);
    }

    private static HttpResponseMessage BuildUnauthorized() =>
        new(HttpStatusCode.Unauthorized) { ReasonPhrase = "Session expired" };

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage source)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri)
        {
            Version = source.Version,
        };
        if (source.Content is not null)
        {
            var bytes = await source.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var h in source.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        foreach (var h in source.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        return clone;
    }
}
