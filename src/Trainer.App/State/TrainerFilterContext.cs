namespace Trainer.App.State;

/// <summary>
/// Singleton state holding the "filter to this trainer" selection. Only meaningful for
/// HeadTrainer — regular trainers always see their own data and ignore this. When the
/// selection changes (picker on GroupsPage), <see cref="SelectionChanged"/> fires so
/// other open pages can refresh.
/// </summary>
public class TrainerFilterContext
{
    public Guid? SelectedOwnerTrainerId { get; private set; }
    public string SelectedDisplayName { get; private set; } = "Все тренеры";

    /// <summary>
    /// False until the first explicit Select() — used by GroupsPage to default the picker
    /// to the current HeadTrainer (their own clients) on first show, rather than 'Все'.
    /// </summary>
    public bool IsInitialized { get; private set; }

    public event EventHandler? SelectionChanged;

    public void Select(Guid? ownerId, string displayName)
    {
        SelectedOwnerTrainerId = ownerId;
        SelectedDisplayName = displayName;
        IsInitialized = true;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Resets to pristine 'Все тренеры' state. Called on sign-out.</summary>
    public void Reset()
    {
        SelectedOwnerTrainerId = null;
        SelectedDisplayName = "Все тренеры";
        IsInitialized = false;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
