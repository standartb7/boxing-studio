using System.Collections.ObjectModel;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.ViewModels;

public class SessionsViewModel
{
    private readonly ISessionService _sessions;
    private List<Session> _all = new();
    private string _searchText = string.Empty;

    public SessionsViewModel(ISessionService sessions)
    {
        _sessions = sessions;
        Items = new ObservableCollection<Session>();
    }

    public Guid? FilterTypeId { get; set; }

    public string SearchText
    {
        get => _searchText;
        set { _searchText = value ?? string.Empty; ApplyFilter(); }
    }

    public ObservableCollection<Session> Items { get; }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (FilterTypeId is null)
        {
            _all = new();
            ApplyFilter();
            return;
        }

        _all = (await _sessions.GetByTypeAsync(FilterTypeId.Value, ct)).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Items.Clear();
        IEnumerable<Session> source = _all;

        var q = _searchText.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            source = _all.Where(s =>
                s.DisplayTitle.Contains(q, StringComparison.OrdinalIgnoreCase)
                || s.Members.Any(m => m.Name.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var s in source) Items.Add(s);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _sessions.DeleteAsync(id, ct);
        _all.RemoveAll(s => s.Id == id);
        var existing = Items.FirstOrDefault(s => s.Id == id);
        if (existing is not null) Items.Remove(existing);
    }
}
