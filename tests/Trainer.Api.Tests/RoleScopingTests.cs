using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Trainer.Contracts;
using Trainer.Core.Entities;

namespace Trainer.Api.Tests;

public class RoleScopingTests : IAsyncLifetime
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

    private void AuthAs(User user) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestData.IssueAccessToken(_factory, user));

    // ---- Clients are gym-wide --------------------------------------------

    [Fact]
    public async Task All_trainers_see_all_clients()
    {
        var a = await TestData.SeedUserAsync(_factory, "a@gym.test", "pw", UserRole.Trainer);
        await TestData.SeedUserAsync(_factory, "b@gym.test", "pw", UserRole.Trainer);
        await TestData.SeedClientAsync(_factory, "Alice");
        await TestData.SeedClientAsync(_factory, "Bob");

        AuthAs(a);
        var list = await _client.GetFromJsonAsync<List<ClientDto>>("/api/clients");
        Assert.Equal(2, list!.Count);
    }

    [Fact]
    public async Task Trainer_can_create_a_client()
    {
        var trainer = await TestData.SeedUserAsync(_factory, "a@gym.test", "pw", UserRole.Trainer);
        AuthAs(trainer);

        var resp = await _client.PostAsJsonAsync("/api/clients", new CreateClientRequest
        {
            Name = "Eve",
            Phone = "+7 999",
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<ClientDto>();
        Assert.Equal("Eve", dto!.Name);
    }

    // ---- Sessions are still per-trainer ----------------------------------

    [Fact]
    public async Task Trainer_only_sees_own_sessions()
    {
        var a = await TestData.SeedUserAsync(_factory, "a@gym.test", "pw", UserRole.Trainer);
        var b = await TestData.SeedUserAsync(_factory, "b@gym.test", "pw", UserRole.Trainer);
        await TestData.SeedSessionAsync(_factory, a.Id, "A's morning");
        await TestData.SeedSessionAsync(_factory, b.Id, "B's evening");

        AuthAs(a);
        var list = await _client.GetFromJsonAsync<List<SessionDto>>("/api/sessions");
        Assert.Single(list!);
        Assert.Equal("A's morning", list![0].Title);
    }

    [Fact]
    public async Task HeadTrainer_sees_all_sessions_with_owner_display_name()
    {
        var head = await TestData.SeedUserAsync(_factory, "h@gym.test", "pw", UserRole.HeadTrainer, displayName: "Head");
        var trainer = await TestData.SeedUserAsync(_factory, "t@gym.test", "pw", UserRole.Trainer, displayName: "Trainer");
        await TestData.SeedSessionAsync(_factory, head.Id, "Head session");
        await TestData.SeedSessionAsync(_factory, trainer.Id, "Trainer session");

        AuthAs(head);
        var list = await _client.GetFromJsonAsync<List<SessionDto>>("/api/sessions");
        Assert.Equal(2, list!.Count);
        Assert.All(list, s => Assert.False(string.IsNullOrEmpty(s.OwnerDisplayName)));
    }

    [Fact]
    public async Task HeadTrainer_can_filter_sessions_by_ownerTrainerId()
    {
        var head = await TestData.SeedUserAsync(_factory, "h@gym.test", "pw", UserRole.HeadTrainer);
        var trainer = await TestData.SeedUserAsync(_factory, "t@gym.test", "pw", UserRole.Trainer);
        await TestData.SeedSessionAsync(_factory, head.Id, "Head's");
        await TestData.SeedSessionAsync(_factory, trainer.Id, "Trainer's");

        AuthAs(head);
        var list = await _client.GetFromJsonAsync<List<SessionDto>>(
            $"/api/sessions?ownerTrainerId={trainer.Id}");
        Assert.Single(list!);
        Assert.Equal("Trainer's", list![0].Title);
    }

    [Fact]
    public async Task Trainer_cannot_filter_sessions_to_another_trainer()
    {
        var a = await TestData.SeedUserAsync(_factory, "a@gym.test", "pw", UserRole.Trainer);
        var b = await TestData.SeedUserAsync(_factory, "b@gym.test", "pw", UserRole.Trainer);

        AuthAs(a);
        var resp = await _client.GetAsync($"/api/sessions?ownerTrainerId={b.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ---- User self-demotion guard (unchanged) ----------------------------

    [Fact]
    public async Task Last_active_head_trainer_cannot_be_demoted()
    {
        var solo = await TestData.SeedUserAsync(_factory, "h@gym.test", "pw", UserRole.HeadTrainer);

        AuthAs(solo);
        var resp = await _client.PutAsJsonAsync($"/api/users/{solo.Id}",
            new UpdateUserRequest { Role = "Trainer" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Demote_allowed_when_another_head_trainer_remains_active()
    {
        var headA = await TestData.SeedUserAsync(_factory, "h1@gym.test", "pw", UserRole.HeadTrainer);
        var headB = await TestData.SeedUserAsync(_factory, "h2@gym.test", "pw", UserRole.HeadTrainer);

        AuthAs(headA);
        var resp = await _client.PutAsJsonAsync($"/api/users/{headB.Id}",
            new UpdateUserRequest { Role = "Trainer" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
