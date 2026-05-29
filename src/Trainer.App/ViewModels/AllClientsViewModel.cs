using System.Collections.ObjectModel;
using Trainer.App.Api;
using Trainer.Contracts;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.ViewModels;

/// <summary>
/// Backing model for [[all-clients-page]] — the HeadTrainer-only "all clients" screen.
///
/// Designed to scale: we load every client once + every session once, build an in-memory
/// map clientId → list of (sessionTitle, ownerName), then filter purely client-side. For
/// ~5k clients this is fast: search is just String.Contains over an in-memory list.
/// </summary>
public class AllClientsViewModel
{
    private readonly IClientService _clients;
    private readonly ISessionApi _sessionsApi;

    private List<ClientCard> _all = new();
    private string _searchText = string.Empty;

    public AllClientsViewModel(IClientService clients, ISessionApi sessionsApi)
    {
        _clients = clients;
        _sessionsApi = sessionsApi;
    }

    public ObservableCollection<ClientCard> Filtered { get; } = new();

    public int TotalCount => _all.Count;
    public int FilteredCount => Filtered.Count;

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value ?? string.Empty;
            ApplyFilter();
        }
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        // Two requests in parallel — clients list and sessions list.
        var clientsTask = _clients.GetAllAsync(ct);
        // ownerTrainerId: null = all trainers (HeadTrainer only — server enforces).
        var sessionsTask = _sessionsApi.GetAllAsync(trainingTypeId: null, ownerTrainerId: null, ct: ct);
        await Task.WhenAll(clientsTask, sessionsTask);

        var allClients = await clientsTask;
        var allSessions = await sessionsTask;

        // Build the clientId → sessions map once.
        var membership = new Dictionary<Guid, List<ClientSessionRef>>(allClients.Count);
        foreach (var s in allSessions)
        {
            var titleForDisplay = string.IsNullOrWhiteSpace(s.Title) ? "Без названия" : s.Title;
            var ownerName = string.IsNullOrWhiteSpace(s.OwnerDisplayName) ? "—" : s.OwnerDisplayName;
            foreach (var cid in s.MemberIds)
            {
                if (!membership.TryGetValue(cid, out var list))
                {
                    list = new List<ClientSessionRef>();
                    membership[cid] = list;
                }
                list.Add(new ClientSessionRef(s.Id, titleForDisplay, ownerName));
            }
        }

        _all = allClients
            .OrderBy(c => c.Name)
            .Select(c =>
            {
                membership.TryGetValue(c.Id, out var sessions);
                return new ClientCard(c, sessions ?? new List<ClientSessionRef>());
            })
            .ToList();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var q = _searchText.Trim();

        Filtered.Clear();
        IEnumerable<ClientCard> source = _all;
        if (!string.IsNullOrEmpty(q))
        {
            source = _all.Where(card =>
                card.Client.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (card.Client.Phone?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var card in source) Filtered.Add(card);
    }
}

public record ClientSessionRef(Guid SessionId, string Title, string OwnerName);

/// <summary>
/// Display-shaped row for [[all-clients-page]]. Precomputes everything the row binding needs
/// so we don't run string formatting on the UI thread for every visible cell.
/// </summary>
public class ClientCard
{
    public Client Client { get; }
    public IReadOnlyList<ClientSessionRef> Sessions { get; }

    public ClientCard(Client client, IReadOnlyList<ClientSessionRef> sessions)
    {
        Client = client;
        Sessions = sessions;
    }

    public string NameUpper => Client.Name.ToUpperInvariant();
    public string Phone => Client.Phone ?? string.Empty;
    public bool HasPhone => !string.IsNullOrWhiteSpace(Client.Phone);

    public string CountDisplay => Sessions.Count.ToString("00");

    /// <summary>Small red caption shown under the name.</summary>
    public string MetaLine
    {
        get
        {
            if (Sessions.Count == 0) return "■ NO SESSIONS";

            var uniqueOwners = Sessions
                .Select(s => s.OwnerName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var sessionWord = SessionWord(Sessions.Count);
            if (uniqueOwners.Count == 1)
                return $"■ {Sessions.Count} {sessionWord} · ◆ {AbbreviateName(uniqueOwners[0]).ToUpperInvariant()}";

            return $"■ {Sessions.Count} {sessionWord} · ◆ {uniqueOwners.Count} ТРЕНЕРА";
        }
    }

    /// <summary>Comma-separated list of "ТРЕНЕР: НАЗВАНИЕ" for the detail sheet (future use).</summary>
    public string SessionsDetailLine =>
        string.Join(" · ", Sessions.Select(s => $"{AbbreviateName(s.OwnerName).ToUpperInvariant()} / {s.Title.ToUpperInvariant()}"));

    private static string SessionWord(int n)
    {
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

    private static string AbbreviateName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "—";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0];
        return $"{parts[1]} {parts[0][..1]}.";
    }
}
