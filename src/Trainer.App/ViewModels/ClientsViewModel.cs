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

    public TrainingType? Filter { get; set; }

    public ObservableCollection<Client> Items { get; }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var data = Filter is null
            ? await _clients.GetAllAsync(ct)
            : await _clients.GetByTypeAsync(Filter.Value, ct);

        Items.Clear();
        foreach (var c in data) Items.Add(c);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _clients.DeleteAsync(id, ct);
        var existing = Items.FirstOrDefault(c => c.Id == id);
        if (existing is not null) Items.Remove(existing);
    }
}
