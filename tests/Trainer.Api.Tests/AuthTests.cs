using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Trainer.Contracts;
using Trainer.Core.Entities;

namespace Trainer.Api.Tests;

public class AuthTests : IAsyncLifetime
{
    private TestApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new TestApiFactory();
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Login_with_correct_credentials_returns_tokens()
    {
        await TestData.SeedUserAsync(_factory, "head@gym.test", "P@ssw0rd!", UserRole.HeadTrainer);

        var resp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "head@gym.test",
            Password = "P@ssw0rd!",
            DeviceId = "device-1",
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
        Assert.False(string.IsNullOrEmpty(body.RefreshToken));
        Assert.Equal("HeadTrainer", body.User.Role);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        await TestData.SeedUserAsync(_factory, "t@gym.test", "good", UserRole.Trainer);

        var resp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "t@gym.test",
            Password = "bad",
            DeviceId = "device-1",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Login_for_unknown_email_returns_401_same_as_bad_password()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "ghost@gym.test",
            Password = "whatever",
            DeviceId = "device-1",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_chain_revokes_on_replay()
    {
        await TestData.SeedUserAsync(_factory, "t@gym.test", "pw", UserRole.Trainer);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "t@gym.test", Password = "pw", DeviceId = "device-1",
        });
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
        var firstRefresh = loginBody!.RefreshToken;

        var rotated = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            RefreshToken = firstRefresh, DeviceId = "device-1",
        });
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var rotatedBody = await rotated.Content.ReadFromJsonAsync<TokenPair>();
        Assert.NotNull(rotatedBody);
        Assert.NotEqual(firstRefresh, rotatedBody!.RefreshToken);

        // Replay of the original (now revoked) token must be rejected.
        var replay = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            RefreshToken = firstRefresh, DeviceId = "device-1",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // The rotated token has also been revoked (chain protection).
        var afterChainRevoke = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            RefreshToken = rotatedBody.RefreshToken, DeviceId = "device-1",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, afterChainRevoke.StatusCode);
    }

    [Fact]
    public async Task Invite_requires_head_trainer_role()
    {
        var trainer = await TestData.SeedUserAsync(_factory, "t@gym.test", "pw", UserRole.Trainer);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestData.IssueAccessToken(_factory, trainer));

        var resp = await _client.PostAsJsonAsync("/api/auth/invite", new InviteRequest
        {
            Email = "x@gym.test", DisplayName = "X", Role = "Trainer",
        });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Invite_and_accept_invite_yields_a_working_session()
    {
        var head = await TestData.SeedUserAsync(_factory, "head@gym.test", "pw", UserRole.HeadTrainer);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestData.IssueAccessToken(_factory, head));

        var invite = await _client.PostAsJsonAsync("/api/auth/invite", new InviteRequest
        {
            Email = "newbie@gym.test", DisplayName = "Newbie", Role = "Trainer",
        });
        Assert.Equal(HttpStatusCode.OK, invite.StatusCode);
        var inviteBody = await invite.Content.ReadFromJsonAsync<InviteResponse>();
        Assert.NotNull(inviteBody);

        _client.DefaultRequestHeaders.Authorization = null;
        var accept = await _client.PostAsJsonAsync("/api/auth/accept-invite", new AcceptInviteRequest
        {
            InviteCode = inviteBody!.InviteCode,
            Password = "newpw",
            DeviceId = "device-2",
        });
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        var body = await accept.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
        Assert.True(body.User.IsActive);
    }
}
