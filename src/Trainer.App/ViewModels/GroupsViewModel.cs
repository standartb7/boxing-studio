using System.Collections.ObjectModel;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.ViewModels;

public record GroupRow(TrainingType Type, string Title, int Count);

public class GroupsViewModel
{
    private readonly IClientService _clients;

    public GroupsViewModel(IClientService clients)
    {
        _clients = clients;
        Items = new ObservableCollection<GroupRow>();
    }

    public ObservableCollection<GroupRow> Items { get; }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var counts = await _clients.GetCountsByTypeAsync(ct);
        Items.Clear();
        foreach (TrainingType type in Enum.GetValues<TrainingType>())
        {
            counts.TryGetValue(type, out var n);
            Items.Add(new GroupRow(type, type.DisplayName(), n));
        }
    }
}
