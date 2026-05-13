using System.Collections.ObjectModel;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.ViewModels;

public class ClientsViewModel
{
    private readonly IClientService _clients;

    public ClientsViewModel(IClientService clients)
    {
        _clients = clients;
        Items = new ObservableCollection<Client>();
    }

    public ObservableCollection<Client> Items { get; }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var data = await _clients.GetActiveAsync(ct);
        Items.Clear();
        foreach (var c in data)
            Items.Add(c);
    }

    public async Task<Client> AddAsync(string name, CancellationToken ct = default)
    {
        var client = new Client { Name = name, StartDate = DateOnly.FromDateTime(DateTime.Today) };
        await _clients.CreateAsync(client, ct);
        Items.Add(client);
        return client;
    }
}
