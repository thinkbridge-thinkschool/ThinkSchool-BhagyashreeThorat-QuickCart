using FluentAssertions;
using Moq;
using QuickCart.Application.Abstractions;
using QuickCart.Application.Orders;
using QuickCart.Domain.Catalog.Entities;
using QuickCart.Domain.Ordering.Aggregates;
using QuickCart.Domain.Ordering.Entities;
using QuickCart.Domain.Shared.Common;

namespace QuickCart.Tests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<ICartRepository> _cartRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly OrderService _sut;

    private static readonly Guid CatId = Guid.NewGuid();
    private static readonly DateTime Now = DateTime.UtcNow;

    public OrderServiceTests()
    {
        _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sut = new OrderService(_orderRepo.Object, _cartRepo.Object, _productRepo.Object, _dispatcher.Object);
    }

    [Fact]
    public async Task CheckoutAsync_WhenCartIsNull_Throws()
    {
        var userId = Guid.NewGuid();
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((Cart?)null);

        var act = () => _sut.CheckoutAsync(userId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Your cart is empty.");
    }

    [Fact]
    public async Task CheckoutAsync_WhenCartIsEmpty_Throws()
    {
        var userId = Guid.NewGuid();
        var emptyCart = Cart.CreateFor(userId, Now);
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(emptyCart);

        var act = () => _sut.CheckoutAsync(userId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Your cart is empty.");
    }

    [Fact]
    public async Task CheckoutAsync_WhenProductNoLongerExists_Throws()
    {
        var userId = Guid.NewGuid();
        var cart = Cart.CreateFor(userId, Now);
        cart.AddItem(Guid.NewGuid(), 1, Now);
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        // Product lookup returns empty list — product was deleted from catalog
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<Product>());

        var act = () => _sut.CheckoutAsync(userId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Product * does not exist.");
    }

    [Fact]
    public async Task CheckoutAsync_WhenProductIsUnavailable_Throws()
    {
        var userId = Guid.NewGuid();
        var product = new Product(CatId, "Toothpaste", null, 75m, null, 0, Now); // out of stock
        var cart = Cart.CreateFor(userId, Now);
        cart.AddItem(product.ProductId, 1, Now);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<Product> { product });

        var act = () => _sut.CheckoutAsync(userId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not available*");
    }

    [Fact]
    public async Task CheckoutAsync_WithValidCart_CreatesOrderAndClearsCart()
    {
        var userId = Guid.NewGuid();
        var product = new Product(CatId, "Rice", null, 50m, null, 100, Now);
        var cart = Cart.CreateFor(userId, Now);
        cart.AddItem(product.ProductId, 2, Now);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<Product> { product });
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>(), default)).Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var view = await _sut.CheckoutAsync(userId);

        view.TotalAmount.Should().Be(100m); // 50 × 2
        view.Items.Should().ContainSingle();
        cart.Items.Should().BeEmpty(); // cart is cleared after checkout
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderBelongsToAnotherUser_ReturnsNull()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid(); // different user
        var order = Order.Create(ownerId, new[] { new OrderItem(Guid.NewGuid(), 10m, 1) }, Now);
        _orderRepo.Setup(r => r.GetByIdAsync(order.OrderId, default)).ReturnsAsync(order);

        var result = await _sut.GetByIdAsync(callerId, order.OrderId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CancelAsync_WhenOrderNotFound_Throws()
    {
        var userId = Guid.NewGuid();
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Order?)null);

        var act = () => _sut.CancelAsync(userId, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Order not found.");
    }

    [Fact]
    public async Task CancelAsync_WhenOrderBelongsToAnotherUser_Throws()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var order = Order.Create(ownerId, new[] { new OrderItem(Guid.NewGuid(), 10m, 1) }, Now);
        _orderRepo.Setup(r => r.GetByIdAsync(order.OrderId, default)).ReturnsAsync(order);

        var act = () => _sut.CancelAsync(callerId, order.OrderId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Order not found.");
    }
}
