using System.Collections.ObjectModel;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.ViewModels;

public class SessionsViewModel
{
    private readonly ISessionService _sessions;

    public SessionsViewModel(ISessionService sessions)
    {
        _sessions = sessions;
        Items = new ObservableCollection<Session>();
    }

    public Guid? FilterTypeId { get; set; }

    public ObservableCollection<Session> Items { get; }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (FilterTypeId is null)
        {
            Items.Clear();
            return;
        }

        var data = await _sessions.GetByTypeAsync(FilterTypeId.Value, ct);
        Items.Clear();
        foreach (var s in data) Items.Add(s);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _sessions.DeleteAsync(id, ct);
        var existing = Items.FirstOrDefault(s => s.Id == id);
        if (existing is not null) Items.Remove(existing);
    }
}
