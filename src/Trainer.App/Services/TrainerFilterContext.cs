namespace Trainer.App.Services;

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

    public event EventHandler? SelectionChanged;

    public void Select(Guid? ownerId, string displayName)
    {
        SelectedOwnerTrainerId = ownerId;
        SelectedDisplayName = displayName;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        SelectedOwnerTrainerId = null;
        SelectedDisplayName = "Все тренеры";
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
