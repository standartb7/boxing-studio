using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Contracts;
using Trainer.Core.Entities;
using Trainer.Data;

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

    [Fact]
    public async Task Trainer_sees_only_own_clients_on_GET()
    {
        var trainerA = await TestData.SeedUserAsync(_factory, "a@gym.test", "pw", UserRole.Trainer, displayName: "A");
        var trainerB = await TestData.SeedUserAsync(_factory, "b@gym.test", "pw", UserRole.Trainer, displayName: "B");
        await TestData.SeedClientAsync(_factory, trainerA.Id, "Alice");
        await TestData.SeedClientAsync(_factory, trainerB.Id, "Bob");

        AuthAs(trainerA);
        var resp = await _client.GetAsync("/api/clients");
        var list = await resp.Content.ReadFromJsonAsync<List<ClientDto>>();
        Assert.NotNull(list);
        Assert.Single(list!);
        Assert.Equal("Alice", list![0].Name);
    }

    [Fact]
    public async Task HeadTrainer_sees_all_clients()
    {
        var head = await TestData.SeedUserAsync(_factory, "h@gym.test", "pw", UserRole.HeadTrainer);
        var trainer = await TestData.SeedUserAsync(_factory, "t@gym.test", "pw", UserRole.Trainer);
        await TestData.SeedClientAsync(_factory, head.Id, "Carol");
        await TestData.SeedClientAsync(_factory, trainer.Id, "Dave");

        AuthAs(head);
        var resp = await _client.GetAsync("/api/clients");
        var list = await resp.Content.ReadFromJsonAsync<List<ClientDto>>();
        Assert.Equal(2, list!.Count);
    }

    [Fact]
    public async Task Trainer_cannot_assign_client_to_another_owner_on_create()
    {
        var trainerA = await TestData.SeedUserAsync(_factory, "a@gym.test", "pw", UserRole.Trainer);
        var trainerB = await TestData.SeedUserAsync(_factory, "b@gym.test", "pw", UserRole.Trainer);

        AuthAs(trainerA);
        var resp = await _client.PostAsJsonAsync("/api/clients", new CreateClientRequest
        {
            Name = "Eve",
            OwnerTrainerId = trainerB.Id, // ignored — service forces self
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<ClientDto>();
        Assert.Equal(trainerA.Id, dto!.OwnerTrainerId);
    }

    [Fact]
    public async Task Trainer_cannot_reassign_clients()
    {
        var trainerA = await TestData.SeedUserAsync(_factory, "a@gym.test", "pw", UserRole.Trainer);
        var trainerB = await TestData.SeedUserAsync(_factory, "b@gym.test", "pw", UserRole.Trainer);
        var (_, clientId) = await TestData.SeedClientAsync(_factory, trainerA.Id, "Frank");

        AuthAs(trainerA);
        var resp = await _client.PostAsJsonAsync($"/api/clients/{clientId}/reassign",
            new ReassignClientRequest { NewOwnerTrainerId = trainerB.Id });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task HeadTrainer_can_reassign_a_client_to_another_trainer()
    {
        var head = await TestData.SeedUserAsync(_factory, "h@gym.test", "pw", UserRole.HeadTrainer);
        var trainer = await TestData.SeedUserAsync(_factory, "t@gym.test", "pw", UserRole.Trainer);
        var (_, clientId) = await TestData.SeedClientAsync(_factory, head.Id, "Gina");

        AuthAs(head);
        var resp = await _client.PostAsJsonAsync($"/api/clients/{clientId}/reassign",
            new ReassignClientRequest { NewOwnerTrainerId = trainer.Id });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        AuthAs(trainer);
        var list = await _client.GetFromJsonAsync<List<ClientDto>>("/api/clients");
        Assert.Single(list!);
        Assert.Equal("Gina", list![0].Name);
    }

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
