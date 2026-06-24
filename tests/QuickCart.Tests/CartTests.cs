using QuickCart.Domain.Ordering.Aggregates;

namespace QuickCart.Tests;

public class CartTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public void AddItem_NewProduct_AddsLine()
    {
        var cart = Cart.CreateFor(Guid.NewGuid(), Now);
        var product = Guid.NewGuid();

        cart.AddItem(product, 2, Now);

        var item = Assert.Single(cart.Items);
        Assert.Equal(product, item.ProductId);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public void AddItem_ExistingProduct_IncreasesQuantity()
    {
        var cart = Cart.CreateFor(Guid.NewGuid(), Now);
        var product = Guid.NewGuid();

        cart.AddItem(product, 2, Now);
        cart.AddItem(product, 3, Now);

        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items.Single().Quantity);
    }

    [Fact]
    public void UpdateItemQuantity_SetsAbsoluteQuantity()
    {
        var cart = Cart.CreateFor(Guid.NewGuid(), Now);
        var product = Guid.NewGuid();
        cart.AddItem(product, 2, Now);

        cart.UpdateItemQuantity(product, 7, Now);

        Assert.Equal(7, cart.Items.Single().Quantity);
    }

    [Fact]
    public void UpdateItemQuantity_MissingProduct_Throws()
    {
        var cart = Cart.CreateFor(Guid.NewGuid(), Now);
        Assert.Throws<InvalidOperationException>(() => cart.UpdateItemQuantity(Guid.NewGuid(), 1, Now));
    }

    [Fact]
    public void RemoveItem_RemovesLine()
    {
        var cart = Cart.CreateFor(Guid.NewGuid(), Now);
        var product = Guid.NewGuid();
        cart.AddItem(product, 2, Now);

        cart.RemoveItem(product, Now);

        Assert.Empty(cart.Items);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var cart = Cart.CreateFor(Guid.NewGuid(), Now);
        cart.AddItem(Guid.NewGuid(), 1, Now);
        cart.AddItem(Guid.NewGuid(), 1, Now);

        cart.Clear(Now);

        Assert.Empty(cart.Items);
    }
}
