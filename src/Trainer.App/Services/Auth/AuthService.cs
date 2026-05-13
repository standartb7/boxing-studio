using System.Net.Http.Json;

namespace Trainer.App.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly AuthState _state;

    public AuthService(IHttpClientFactory factory, AuthState state)
    {
        // namedClient "auth" — без BearerTokenHandler, чтобы при логине не пробовать вставить пустой токен
        _http = factory.CreateClient("auth");
        _state = state;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string? displayName, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/register",
                new RegisterRequest(email, password, displayName), ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return new AuthResult(false, string.IsNullOrWhiteSpace(error) ? response.ReasonPhrase : error);
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
            if (auth is null) return new AuthResult(false, "Пустой ответ сервера");

            await _state.SetAsync(auth);
            return new AuthResult(true);
        }
        catch (Exception ex)
        {
            return new AuthResult(false, ex.Message);
        }
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/login",
                new LoginRequest(email, password), ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new AuthResult(false, "Неверный email или пароль");

            if (!response.IsSuccessStatusCode)
                return new AuthResult(false, response.ReasonPhrase ?? "Ошибка входа");

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
            if (auth is null) return new AuthResult(false, "Пустой ответ сервера");

            await _state.SetAsync(auth);
            return new AuthResult(true);
        }
        catch (Exception ex)
        {
            return new AuthResult(false, ex.Message);
        }
    }

    public Task LogoutAsync()
    {
        _state.Clear();
        return Task.CompletedTask;
    }
}
