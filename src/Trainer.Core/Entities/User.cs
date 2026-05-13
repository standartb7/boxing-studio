namespace Trainer.Core.Entities;

/// <summary>
/// Пользователь системы. На этапе 2 действует модель «1 тренер = 1 тенант»:
/// при регистрации каждому новому пользователю выдаётся свой собственный TenantId.
/// Все его данные (клиенты, упражнения, расписание) автоматически фильтруются этим Id.
/// </summary>
public class User : EntityBase
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
