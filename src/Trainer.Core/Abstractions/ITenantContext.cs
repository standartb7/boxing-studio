namespace Trainer.Core.Abstractions;

public interface ITenantContext
{
    Guid CurrentTenantId { get; }
}
