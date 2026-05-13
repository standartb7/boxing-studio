using System.Net.Http.Headers;
using Trainer.App.Services.Auth;

namespace Trainer.App.Services.Api;

public class BearerTokenHandler : DelegatingHandler
{
    private readonly AuthState _state;

    public BearerTokenHandler(AuthState state)
    {
        _state = state;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_state.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _state.Token);
        }

        var response = await base.SendAsync(request, ct);

        // Токен протух или украден — выкидываем на логин.
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && _state.IsAuthenticated)
        {
            _state.Clear();
        }

        return response;
    }
}
