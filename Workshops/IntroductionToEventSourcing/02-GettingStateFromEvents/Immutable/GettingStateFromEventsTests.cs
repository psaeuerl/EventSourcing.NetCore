using FluentAssertions;
using Xunit;

namespace IntroductionToEventSourcing.GettingStateFromEvents.Immutable;

using static ShoppingCartEvent;

// EVENTS
public abstract record ShoppingCartEvent
{
    // This won't allow external inheritance
    private ShoppingCartEvent() { }

    public record ShoppingCartOpened(
        Guid ShoppingCartId,
        Guid ClientId
    ): ShoppingCartEvent;

    public record ProductItemAddedToShoppingCart(
        Guid ShoppingCartId,
        PricedProductItem ProductItem
    ): ShoppingCartEvent;

    public record ProductItemRemovedFromShoppingCart(
        Guid ShoppingCartId,
        PricedProductItem ProductItem
    ): ShoppingCartEvent;

    public record ShoppingCartConfirmed(
        Guid ShoppingCartId,
        DateTime ConfirmedAt
    ): ShoppingCartEvent;

    public record ShoppingCartCanceled(
        Guid ShoppingCartId,
        DateTime CanceledAt
    ): ShoppingCartEvent;
}

// VALUE OBJECTS
public record PricedProductItem(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);

// ENTITY
public record ShoppingCart(
    Guid Id,
    Guid ClientId,
    ShoppingCartStatus Status,
    PricedProductItem[] ProductItems,
    DateTime? ConfirmedAt = null,
    DateTime? CanceledAt = null
)
{
    public static ShoppingCart From(IEnumerable<ShoppingCartEvent> events)
    {
        var shoppingCart = new ShoppingCart(default, default, default, Array.Empty<PricedProductItem>());
        foreach (var shoppingCartEvent in events) shoppingCart = shoppingCart.Apply(shoppingCartEvent);
        return shoppingCart;
    }

    public ShoppingCart Apply(ShoppingCartEvent @event)
    {
        switch (@event)
        {
            case ProductItemAddedToShoppingCart productItemAddedToShoppingCart:
                return this with
                {
                    ProductItems = new[] { productItemAddedToShoppingCart.ProductItem }.Concat(ProductItems)
                        .ToArray()
                };
            case ProductItemRemovedFromShoppingCart productItemRemovedFromShoppingCart:
                var productItemProductId = productItemRemovedFromShoppingCart.ProductItem.ProductId;
                var amountStored = ProductItems.Single(x => x.ProductId == productItemProductId)
                    .Quantity;
                var newQuantity = amountStored - productItemRemovedFromShoppingCart.ProductItem.Quantity;
                var productItemsWithoutOld = ProductItems
                    .Where(x => x.ProductId != productItemProductId).ToArray();
                if (newQuantity < 0)
                {
                    return this with { ProductItems = productItemsWithoutOld };
                }

                var productItemsUpdated = new[]
                {
                    new PricedProductItem(productItemProductId, newQuantity,
                        productItemRemovedFromShoppingCart.ProductItem.UnitPrice)
                }.Concat(productItemsWithoutOld).ToArray();

                return this with { ProductItems = productItemsUpdated };
            case ShoppingCartCanceled shoppingCartCanceled:
                return this with
                {
                    Status = ShoppingCartStatus.Canceled,
                    CanceledAt = shoppingCartCanceled.CanceledAt,
                    ConfirmedAt = null
                };
            case ShoppingCartConfirmed shoppingCartConfirmed:
                return this with
                {
                    Status = ShoppingCartStatus.Confirmed,
                    ConfirmedAt = shoppingCartConfirmed.ConfirmedAt,
                    CanceledAt = null
                };
            case ShoppingCartOpened shoppingCartOpened:
                return new ShoppingCart(
                    shoppingCartOpened.ShoppingCartId
                    , shoppingCartOpened.ClientId,
                    ShoppingCartStatus.Pending,
                    Array.Empty<PricedProductItem>());
            default:
                throw new ArgumentOutOfRangeException(nameof(@event));
        }
    }
}

public enum ShoppingCartStatus
{
    Pending = 1,
    Confirmed = 2,
    Canceled = 4
}

public class GettingStateFromEventsTests
{
    private static ShoppingCart GetShoppingCart(IEnumerable<ShoppingCartEvent> events)
    {
        // 1. Add logic here
        return ShoppingCart.From(events);
    }

    [Fact]
    [Trait("Category", "SkipCI")]
    public void GettingState_ForSequenceOfEvents_ShouldSucceed()
    {
        var shoppingCartId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var shoesId = Guid.NewGuid();
        var tShirtId = Guid.NewGuid();
        var twoPairsOfShoes = new PricedProductItem(shoesId, 2, 100);
        var pairOfShoes = new PricedProductItem(shoesId, 1, 100);
        var tShirt = new PricedProductItem(tShirtId, 1, 50);

        var events = new ShoppingCartEvent[]
        {
            new ShoppingCartOpened(shoppingCartId, clientId),
            new ProductItemAddedToShoppingCart(shoppingCartId, twoPairsOfShoes),
            new ProductItemAddedToShoppingCart(shoppingCartId, tShirt),
            new ProductItemRemovedFromShoppingCart(shoppingCartId, pairOfShoes),
            new ShoppingCartConfirmed(shoppingCartId, DateTime.UtcNow),
            new ShoppingCartCanceled(shoppingCartId, DateTime.UtcNow)
        };

        var shoppingCart = GetShoppingCart(events);

        shoppingCart.Id.Should().Be(shoppingCartId);
        shoppingCart.ClientId.Should().Be(clientId);
        shoppingCart.ProductItems.Should().HaveCount(2);
        shoppingCart.ProductItems[0].Should().Be(pairOfShoes);
        shoppingCart.ProductItems[1].Should().Be(tShirt);
    }
}
