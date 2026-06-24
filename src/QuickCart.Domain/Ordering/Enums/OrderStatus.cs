namespace QuickCart.Domain.Ordering.Enums;

/// <summary>
/// Order lifecycle for the current scope. There is no payment gateway and no delivery workflow,
/// so an order is <see cref="Confirmed"/> the moment it is placed and may be <see cref="Cancelled"/>.
/// </summary>
public enum OrderStatus
{
    Confirmed,
    Cancelled
}
