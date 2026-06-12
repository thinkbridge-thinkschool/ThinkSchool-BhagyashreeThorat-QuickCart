using QuickCart.Domain.Ordering.Aggregates;
using QuickCart.Domain.Ordering.Entities;
using QuickCart.Domain.Ordering.Enums;
using QuickCart.Domain.Ordering.Events;

namespace QuickCart.Tests;

public class OrderTests
{
    private static OrderLine SampleLine(decimal price = 10m, int qty = 2) =>
        new(Guid.NewGuid(), "Widget", price, qty);

    [Fact]
    public void Create_WithLines_StartsSubmitted_AndRaisesOrderCreated()
    {
        var order = Order.Create(Guid.NewGuid(), new[] { SampleLine() }, DateTime.UtcNow);

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal(20m, order.Total);
        Assert.Single(order.DomainEvents);
        Assert.IsType<OrderCreatedEvent>(order.DomainEvents.Single());
    }

    [Fact]
    public void Create_WithNoLines_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => Order.Create(Guid.NewGuid(), Array.Empty<OrderLine>(), DateTime.UtcNow));
    }

    [Fact]
    public void MarkPaid_FromSubmitted_TransitionsAndRaisesPaymentSucceeded()
    {
        var order = Order.Create(Guid.NewGuid(), new[] { SampleLine() }, DateTime.UtcNow);
        order.ClearDomainEvents();

        order.MarkPaid(DateTime.UtcNow);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.IsType<PaymentSucceededEvent>(order.DomainEvents.Single());
    }

    [Fact]
    public void MarkPaid_Twice_Throws()
    {
        var order = Order.Create(Guid.NewGuid(), new[] { SampleLine() }, DateTime.UtcNow);
        order.MarkPaid(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => order.MarkPaid(DateTime.UtcNow));
    }
}
