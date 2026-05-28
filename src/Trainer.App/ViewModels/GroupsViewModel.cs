using System.Collections.ObjectModel;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.ViewModels;

public class GroupRow
{
    public TrainingType Type { get; init; } = null!;
    public int Count { get; init; }
    public string RowIndex { get; set; } = string.Empty;

    public string CountDisplay => Count.ToString("00");

    // Underground redesign meta line: small red caption under the group name.
    // We don't track type-category / trainer / schedule on TrainingType itself,
    // so we keep it minimal but informative.
    public string MetaLine =>
        Count == 0
            ? "■ GROUP · ПУСТО"
            : $"■ GROUP · {Count} {SessionWord(Count)}";

    private static string SessionWord(int n)
    {
        // Russian plural: 1 сессия, 2–4 сессии, 5+ сессий.
        var mod10 = n % 10;
        var mod100 = n % 100;
        if (mod100 >= 11 && mod100 <= 14) return "СЕССИЙ";
        return mod10 switch
        {
            1 => "СЕССИЯ",
            2 or 3 or 4 => "СЕССИИ",
            _ => "СЕССИЙ",
        };
    }
}

public class GroupsViewModel
{
    private readonly ITrainingTypeService _types;
    private readonly ISessionService _sessions;

    public GroupsViewModel(ITrainingTypeService types, ISessionService sessions)
    {
        _types = types;
        _sessions = sessions;
        Items = new ObservableCollection<GroupRow>();
    }

    public ObservableCollection<GroupRow> Items { get; }

    public int TotalSessionCount => Items.Sum(r => r.Count);

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var types = await _types.GetAllAsync(ct);
        var counts = await _sessions.GetCountsByTypeAsync(ct);

        Items.Clear();
        var idx = 1;
        foreach (var t in types)
        {
            counts.TryGetValue(t.Id, out var n);
            Items.Add(new GroupRow
            {
                Type = t,
                Count = n,
                RowIndex = idx.ToString("00"),
            });
            idx++;
        }
    }

    public async Task<TrainingType> AddGroupAsync(string name, CancellationToken ct = default)
    {
        var created = await _types.CreateOrGetAsync(name, ct);
        await LoadAsync(ct);
        return created;
    }

    public async Task DeleteGroupAsync(Guid id, CancellationToken ct = default)
    {
        await _types.DeleteAsync(id, ct);
        var row = Items.FirstOrDefault(r => r.Type.Id == id);
        if (row is not null) Items.Remove(row);
    }
}
