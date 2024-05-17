using FluentAssertions;
using Xunit;

namespace IntroductionToEventSourcing.EventsDefinition;

// 1. Define your events and entity here
//1. The customer may add a product to the shopping cart only after opening it.
//2. When selecting and adding a product to the basket customer needs to provide the quantity chosen. The product price is calculated by the system based on the current price list.
//3. The customer may remove a product with a given price from the cart.
//4. The customer can confirm the shopping cart and start the order fulfilment process.
//5. The customer may also cancel the shopping cart and reject all selected products.
//6. After shopping cart confirmation or cancellation, the product can no longer be added or removed from the cart.

//Events
public abstract record ShoppingCartEvent
{
    public record ShoppingCartOpened(
        Guid ShoppingCartId,
        Guid ClientId);

    public record ProductAdded(Guid ShoppingCartId, PricedProduct Product);

    public record ProductRemoved(Guid ShoppingCartId, PricedProduct Product);

    public record ShoppingCartConfirmed(Guid ShoppingCartId, DateTime ConfirmedAt);

    public record ShoppingCardCanceled(Guid ShoppingCartId, DateTime CanceledAt);

    // This won't allow external inheritance
    private ShoppingCartEvent() { }
}

//Value Objects

public record PricedProduct(Guid ProductId, int price, int quantity)
{
    public int TotalPrice => price * quantity;
}

//Entities
public record ShoppingCart(Guid ShoppingCartId, Guid ClientId, PricedProduct[] Products, ShoppingCartStatus Status, DateTime? ConfirmedAt = null,
    DateTime? CanceledAt = null);

public enum ShoppingCartStatus
{
    Pending,
    Confirmed,
    Cancelled

}

public class EventsDefinitionTests
{
    [Fact]
    [Trait("Category", "SkipCI")]
    public void AllEventTypes_ShouldBeDefined()
    {
        var shoppingCartId = Guid.NewGuid();
        var firstProductId = Guid.NewGuid();
        var tenCards = new PricedProduct(firstProductId,10,20);
        var events = new object[]
        {
            // 2. Put your sample events here
            new ShoppingCartEvent.ShoppingCartOpened(shoppingCartId,Guid.NewGuid()),
            new ShoppingCartEvent.ProductAdded(shoppingCartId,tenCards),
            new ShoppingCartEvent.ProductRemoved(shoppingCartId,tenCards),
            new ShoppingCartEvent.ShoppingCartConfirmed(shoppingCartId,DateTime.Now),
            new ShoppingCartEvent.ShoppingCardCanceled(shoppingCartId,DateTime.Now),
        };

        const int expectedEventTypesCount = 5;
        events.Should().HaveCount(expectedEventTypesCount);
        events.GroupBy(e => e.GetType()).Should().HaveCount(expectedEventTypesCount);
    }
}
