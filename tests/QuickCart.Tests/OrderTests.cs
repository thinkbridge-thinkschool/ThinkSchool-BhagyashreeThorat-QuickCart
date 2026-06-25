using QuickCart.Domain.Ordering.Aggregates;
using QuickCart.Domain.Ordering.Entities;
using QuickCart.Domain.Ordering.Enums;
using QuickCart.Domain.Ordering.Events;

namespace QuickCart.Tests;

public class OrderTests
{
    private static OrderItem SampleItem(decimal price = 10m, int qty = 2) =>
        new(Guid.NewGuid(), price, qty);

    [Fact]
    public void Create_WithItems_StartsConfirmed_SetsTotalAndRaisesOrderCreated()
    {
        var order = Order.Create(Guid.NewGuid(), new[] { SampleItem() }, DateTime.UtcNow);

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(20m, order.TotalAmount);
        Assert.IsType<OrderCreatedEvent>(order.DomainEvents.Single());
    }

    [Fact]
    public void Create_WithMultipleItems_SumsTotalCorrectly()
    {
        var items = new[]
        {
            SampleItem(price: 5m, qty: 2),  // 10
            SampleItem(price: 3m, qty: 3),  // 9
        };
        var order = Order.Create(Guid.NewGuid(), items, DateTime.UtcNow);

        Assert.Equal(19m, order.TotalAmount);
        Assert.Equal(2, order.Items.Count);
    }

    [Fact]
    public void Create_WithNoItems_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => Order.Create(Guid.NewGuid(), Array.Empty<OrderItem>(), DateTime.UtcNow));
    }

    [Fact]
    public void Cancel_ConfirmedOrder_BecomesCancel()
    {
        var order = Order.Create(Guid.NewGuid(), new[] { SampleItem() }, DateTime.UtcNow);
        order.Cancel(DateTime.UtcNow);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_IsIdempotent()
    {
        var order = Order.Create(Guid.NewGuid(), new[] { SampleItem() }, DateTime.UtcNow);
        order.Cancel(DateTime.UtcNow);
        order.Cancel(DateTime.UtcNow); // should not throw

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }
}
