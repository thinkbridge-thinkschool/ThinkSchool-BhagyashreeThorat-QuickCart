namespace QuickCart.Domain.Shared.Common;

/// <summary>
/// Audit + soft-delete fields shared by entities. It deliberately does NOT define the primary
/// key: each entity declares its own explicit key (OrderId, ProductId, ...) per the design's
/// naming convention, so there is no generic Id property to collide with.
/// </summary>
public abstract class BaseEntity
{
    public DateTime CreatedAtUtc { get; protected set; }
    public DateTime? ModifiedAtUtc { get; protected set; }
    public bool IsDeleted { get; protected set; }

    /// <summary>Stamp the creation time. Called once from an entity's factory/constructor.</summary>
    protected void MarkCreated(DateTime utcNow) => CreatedAtUtc = utcNow;

    /// <summary>Record that the entity changed. Called from behaviour that mutates state.</summary>
    protected void MarkModified(DateTime utcNow) => ModifiedAtUtc = utcNow;

    /// <summary>Soft-delete the entity rather than removing the row.</summary>
    public void SoftDelete(DateTime utcNow)
    {
        IsDeleted = true;
        ModifiedAtUtc = utcNow;
    }
}
