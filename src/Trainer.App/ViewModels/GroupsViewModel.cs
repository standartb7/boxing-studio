using System.Collections.ObjectModel;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.ViewModels;

public record GroupRow(TrainingType Type, int Count);

public class GroupsViewModel
{
    private readonly ITrainingTypeService _types;
    private readonly IClientService _clients;

    public GroupsViewModel(ITrainingTypeService types, IClientService clients)
    {
        _types = types;
        _clients = clients;
        Items = new ObservableCollection<GroupRow>();
    }

    public ObservableCollection<GroupRow> Items { get; }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var types = await _types.GetAllAsync(ct);
        var counts = await _clients.GetCountsByTypeAsync(ct);

        Items.Clear();
        foreach (var t in types)
        {
            counts.TryGetValue(t.Id, out var n);
            Items.Add(new GroupRow(t, n));
        }
    }

    public async Task<TrainingType> AddGroupAsync(string name, CancellationToken ct = default)
    {
        var created = await _types.CreateOrGetAsync(name, ct);
        await LoadAsync(ct);
        return created;
    }
}
