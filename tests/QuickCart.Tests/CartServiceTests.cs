using FluentAssertions;
using Moq;
using QuickCart.Application.Abstractions;
using QuickCart.Application.Carts;
using QuickCart.Domain.Catalog.Entities;
using QuickCart.Domain.Ordering.Aggregates;

namespace QuickCart.Tests;

public class CartServiceTests
{
    private readonly Mock<ICartRepository> _cartRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly CartService _sut;

    private static readonly Guid CatId = Guid.NewGuid();
    private static readonly DateTime Now = DateTime.UtcNow;

    public CartServiceTests() => _sut = new CartService(_cartRepo.Object, _productRepo.Object);

    [Fact]
    public async Task GetCartAsync_WhenNoCartExists_ReturnsEmptyCart()
    {
        var userId = Guid.NewGuid();
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((Cart?)null);

        var view = await _sut.GetCartAsync(userId);

        view.Lines.Should().BeEmpty();
        view.Total.Should().Be(0m);
    }

    [Fact]
    public async Task AddItemAsync_WhenProductNotFound_Throws()
    {
        var userId = Guid.NewGuid();
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Product?)null);

        var act = () => _sut.AddItemAsync(userId, missingId, 1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Product does not exist.");
    }

    [Fact]
    public async Task AddItemAsync_WhenProductIsUnavailable_Throws()
    {
        var userId = Guid.NewGuid();
        // StockQuantity=0 → IsAvailable=false
        var outOfStock = new Product(CatId, "Toothpaste", null, 75m, null, 0, Now);
        _productRepo.Setup(r => r.GetByIdAsync(outOfStock.ProductId, default)).ReturnsAsync(outOfStock);

        var act = () => _sut.AddItemAsync(userId, outOfStock.ProductId, 1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Product is not available.");
    }

    [Fact]
    public async Task AddItemAsync_WhenProductAvailable_ReturnsCartWithItem()
    {
        var userId = Guid.NewGuid();
        var product = new Product(CatId, "Rice", null, 10m, null, 100, Now);

        _productRepo.Setup(r => r.GetByIdAsync(product.ProductId, default)).ReturnsAsync(product);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<Product> { product });
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((Cart?)null);
        _cartRepo.Setup(r => r.AddAsync(It.IsAny<Cart>(), default)).Returns(Task.CompletedTask);
        _cartRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var view = await _sut.AddItemAsync(userId, product.ProductId, 3);

        view.Lines.Should().ContainSingle();
        view.Lines[0].Quantity.Should().Be(3);
        view.Total.Should().Be(30m);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenCartNotFound_Throws()
    {
        var userId = Guid.NewGuid();
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((Cart?)null);

        var act = () => _sut.UpdateItemAsync(userId, Guid.NewGuid(), 5);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cart is empty.");
    }

    [Fact]
    public async Task UpdateItemAsync_WhenProductNotInCart_Throws()
    {
        var userId = Guid.NewGuid();
        var cart = Cart.CreateFor(userId, Now);
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);

        var act = () => _sut.UpdateItemAsync(userId, Guid.NewGuid(), 5);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RemoveItemAsync_WhenCartNotFound_ReturnsEmptyCart()
    {
        var userId = Guid.NewGuid();
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((Cart?)null);

        var view = await _sut.RemoveItemAsync(userId, Guid.NewGuid());

        view.Lines.Should().BeEmpty();
    }
}
