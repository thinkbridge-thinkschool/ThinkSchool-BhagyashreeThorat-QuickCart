namespace QuickCart.Domain.Shared.Common;

/// <summary>Marker for something noteworthy that happened inside an aggregate.</summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
