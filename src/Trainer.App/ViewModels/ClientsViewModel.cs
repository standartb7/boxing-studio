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

    public Guid? FilterTypeId { get; set; }

    public ObservableCollection<Client> Items { get; }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var data = FilterTypeId is null
            ? await _clients.GetAllAsync(ct)
            : await _clients.GetByTypeAsync(FilterTypeId.Value, ct);

        Items.Clear();
        foreach (var c in data) Items.Add(c);
    }
}
