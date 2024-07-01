using FluentAssertions;
using IntroductionToEventSourcing.GettingStateFromEvents.Tools;
using Xunit;

namespace IntroductionToEventSourcing.GettingStateFromEvents;

// EVENTS
public record ShoppingCartOpened(
    Guid ShoppingCartId,
    Guid ClientId
);

public record ProductItemAddedToShoppingCart(
    Guid ShoppingCartId,
    PricedProductItem ProductItem
);

public record ProductItemRemovedFromShoppingCart(
    Guid ShoppingCartId,
    PricedProductItem ProductItem
);

public record ShoppingCartConfirmed(
    Guid ShoppingCartId,
    DateTime ConfirmedAt
);

public record ShoppingCartCanceled(
    Guid ShoppingCartId,
    DateTime CanceledAt
);

// VALUE OBJECTS
public record PricedProductItem(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
)
{
    public decimal TotalAmount => Quantity * UnitPrice;
}

public enum ShoppingCartStatus
{
    Pending = 1,
    Confirmed = 2,
    Canceled = 4
}

public class ShoppingCartDetails
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public ShoppingCartStatus Status { get; set; }
    public IList<PricedProductItem> ProductItems { get; set; } = new List<PricedProductItem>();
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TotalItemsCount { get; set; }

    public void Handle(ShoppingCartOpened eventEnvelopeData)
    {
        Id = eventEnvelopeData.ShoppingCartId;
        ClientId = eventEnvelopeData.ClientId;
        Status = ShoppingCartStatus.Pending;
    }

    public void Handle(ProductItemRemovedFromShoppingCart eventEnvelopeData)
    {
        var product = ProductItems.Single(x => x.ProductId == eventEnvelopeData.ProductItem.ProductId);
        var pricedProductItem = product with { Quantity = product.Quantity - eventEnvelopeData.ProductItem.Quantity };
        ProductItems.Remove(product);
        if (pricedProductItem.Quantity > 0)
            ProductItems.Add(pricedProductItem);
        UpdateTotals();
    }

    public void Handle(ProductItemAddedToShoppingCart eventEnvelopeData)
    {
        var product = ProductItems.SingleOrDefault(x => x.ProductId == eventEnvelopeData.ProductItem.ProductId);
        if (product == null)
        {
            ProductItems.Add(eventEnvelopeData.ProductItem);
        }
        else
        {
            var pricedProductItem = product with
            {
                Quantity = product.Quantity + eventEnvelopeData.ProductItem.Quantity
            };
            ProductItems.Remove(product);
            ProductItems.Add(pricedProductItem);
        }

        UpdateTotals();
    }

    private void UpdateTotals()
    {
        TotalPrice = ProductItems.Sum(x => x.TotalAmount);
        TotalItemsCount = ProductItems.Sum(x=>x.TotalAmount);
    }

    public void Handle(ShoppingCartConfirmed eventEnvelopeData)
    {
        Status= ShoppingCartStatus.Confirmed;
        ConfirmedAt = eventEnvelopeData.ConfirmedAt;
    }

    public void Handle(ShoppingCartCanceled eventEnvelopeData)
    {
        Status = ShoppingCartStatus.Canceled;
        CanceledAt = eventEnvelopeData.CanceledAt;
    }
}

public class ShoppingCartShortInfo
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TotalItemsCount { get; set; }

    public void Handle(ShoppingCartOpened eventEnvelopeData)
    {
        Id = eventEnvelopeData.ShoppingCartId;
        ClientId = eventEnvelopeData.ClientId;
        TotalItemsCount = 0;
        TotalPrice = 0;
    }

    public void Handle(ProductItemAddedToShoppingCart eventEnvelopeData)
    {
        TotalPrice += eventEnvelopeData.ProductItem.TotalAmount;
        TotalItemsCount += eventEnvelopeData.ProductItem.Quantity;
    }

    public void Handle(ProductItemRemovedFromShoppingCart eventEnvelopeData)
    {
        TotalPrice -= eventEnvelopeData.ProductItem.TotalAmount;
        TotalItemsCount -= eventEnvelopeData.ProductItem.Quantity;
    }
}

public class ShoppingCartShortInfoProjector
{
    private readonly Database _db;

    public ShoppingCartShortInfoProjector(Database database)
    {
        _db = database;
    }

    public void Handle(EventEnvelope<ShoppingCartOpened> eventEnvelope)
    {
        var scsi = new ShoppingCartShortInfo();
        scsi.Handle(eventEnvelope.Data);
        _db.Store(scsi.Id, scsi);
    }

    public void Handle(EventEnvelope<ProductItemAddedToShoppingCart> eventEnvelope)
    {
        var scsi = _db.Get<ShoppingCartShortInfo>(eventEnvelope.Data.ShoppingCartId) ??
                   throw new ArgumentNullException();
        scsi.Handle(eventEnvelope.Data);
        _db.Store(scsi.Id, scsi);
    }

    public void Handle(EventEnvelope<ProductItemRemovedFromShoppingCart> eventEnvelope)
    {
        var scsi = _db.Get<ShoppingCartShortInfo>(eventEnvelope.Data.ShoppingCartId) ??
                   throw new ArgumentNullException();
        scsi.Handle(eventEnvelope.Data);
        _db.Store(scsi.Id, scsi);
    }

    public void Handle(EventEnvelope<ShoppingCartConfirmed> eventEnvelope)
    {
        _db.Delete< ShoppingCartShortInfo>(eventEnvelope.Data.ShoppingCartId);
    }

    public void Handle(EventEnvelope<ShoppingCartCanceled> eventEnvelope)
    {
        _db.Delete<ShoppingCartShortInfo>(eventEnvelope.Data.ShoppingCartId);
    }
}

public class ShoppingCartDetailsProjector
{
    private readonly Database _db;

    public ShoppingCartDetailsProjector(Database database)
    {
        _db = database;
    }

    public void Handle(EventEnvelope<ShoppingCartOpened> eventEnvelope)
    {
        var scd = new ShoppingCartDetails();
        scd.Handle(eventEnvelope.Data);
        _db.Store(scd.Id, scd);
    }

    public void Handle(EventEnvelope<ProductItemAddedToShoppingCart> eventEnvelope)
    {
        var scd = _db.Get<ShoppingCartDetails>(eventEnvelope.Data.ShoppingCartId) ??
                   throw new ArgumentNullException();
        scd.Handle(eventEnvelope.Data);
        _db.Store(scd.Id, scd);
    }

    public void Handle(EventEnvelope<ProductItemRemovedFromShoppingCart> eventEnvelope)
    {
        var scd = _db.Get<ShoppingCartDetails>(eventEnvelope.Data.ShoppingCartId) ??
                   throw new ArgumentNullException();
        scd.Handle(eventEnvelope.Data);
        _db.Store(scd.Id, scd);
    }

    public void Handle(EventEnvelope<ShoppingCartConfirmed> eventEnvelope)
    {
        var scd = _db.Get<ShoppingCartDetails>(eventEnvelope.Data.ShoppingCartId) ??
                  throw new ArgumentNullException();
        scd.Handle(eventEnvelope.Data);
        _db.Store(scd.Id, scd);
    }

    public void Handle(EventEnvelope<ShoppingCartCanceled> eventEnvelope)
    {
        var scd = _db.Get<ShoppingCartDetails>(eventEnvelope.Data.ShoppingCartId) ??
                  throw new ArgumentNullException();
        scd.Handle(eventEnvelope.Data);
        _db.Store(scd.Id, scd);
    }
}

public class ProjectionsTests
{
    [Fact]
    [Trait("Category", "SkipCI")]
    public void GettingState_ForSequenceOfEvents_ShouldSucceed()
    {
        var shoppingCartId = Guid.NewGuid();

        var clientId = Guid.NewGuid();
        var shoesId = Guid.NewGuid();
        var tShirtId = Guid.NewGuid();
        var dressId = Guid.NewGuid();
        var trousersId = Guid.NewGuid();

        var twoPairsOfShoes = new PricedProductItem(shoesId, 2, 100);
        var pairOfShoes = new PricedProductItem(shoesId, 1, 100);
        var tShirt = new PricedProductItem(tShirtId, 1, 50);
        var dress = new PricedProductItem(dressId, 3, 150);
        var trousers = new PricedProductItem(trousersId, 1, 300);

        var cancelledShoppingCartId = Guid.NewGuid();
        var otherClientShoppingCartId = Guid.NewGuid();
        var otherConfirmedShoppingCartId = Guid.NewGuid();
        var otherPendingShoppingCartId = Guid.NewGuid();
        var otherClientId = Guid.NewGuid();

        var eventStore = new EventStore();
        var database = new Database();

        var ShoppingCartShortInfoProjector = new ShoppingCartShortInfoProjector(database);
        var ShoppingCartDetailsProjector = new ShoppingCartDetailsProjector(database);
        // TODO:
        // 1. Register here your event handlers using `eventBus.Register`.
        // 2. Store results in database.
        eventStore.Register<ShoppingCartOpened>(x => ShoppingCartShortInfoProjector.Handle(x));
        eventStore.Register<ProductItemAddedToShoppingCart>(x => ShoppingCartShortInfoProjector.Handle(x));
        eventStore.Register<ProductItemRemovedFromShoppingCart>(x => ShoppingCartShortInfoProjector.Handle(x));
        eventStore.Register<ShoppingCartConfirmed>(x => ShoppingCartShortInfoProjector.Handle(x));
        eventStore.Register<ShoppingCartCanceled>(x => ShoppingCartShortInfoProjector.Handle(x));


        eventStore.Register<ShoppingCartOpened>(x => ShoppingCartDetailsProjector.Handle(x));
        eventStore.Register<ProductItemAddedToShoppingCart>(x => ShoppingCartDetailsProjector.Handle(x));
        eventStore.Register<ProductItemRemovedFromShoppingCart>(x => ShoppingCartDetailsProjector.Handle(x));
        eventStore.Register<ShoppingCartConfirmed>(x => ShoppingCartDetailsProjector.Handle(x));
        eventStore.Register<ShoppingCartCanceled>(x => ShoppingCartDetailsProjector.Handle(x));

        // first confirmed
        eventStore.Append(shoppingCartId, new ShoppingCartOpened(shoppingCartId, clientId));
        eventStore.Append(shoppingCartId, new ProductItemAddedToShoppingCart(shoppingCartId, twoPairsOfShoes));
        eventStore.Append(shoppingCartId, new ProductItemAddedToShoppingCart(shoppingCartId, tShirt));
        eventStore.Append(shoppingCartId, new ProductItemRemovedFromShoppingCart(shoppingCartId, pairOfShoes));
        eventStore.Append(shoppingCartId, new ShoppingCartConfirmed(shoppingCartId, DateTime.UtcNow));

        // cancelled
        eventStore.Append(cancelledShoppingCartId, new ShoppingCartOpened(cancelledShoppingCartId, clientId));
        eventStore.Append(cancelledShoppingCartId, new ProductItemAddedToShoppingCart(cancelledShoppingCartId, dress));
        eventStore.Append(cancelledShoppingCartId, new ShoppingCartCanceled(cancelledShoppingCartId, DateTime.UtcNow));

        // confirmed but other client
        eventStore.Append(otherClientShoppingCartId, new ShoppingCartOpened(otherClientShoppingCartId, otherClientId));
        eventStore.Append(otherClientShoppingCartId, new ProductItemAddedToShoppingCart(otherClientShoppingCartId, dress));
        eventStore.Append(otherClientShoppingCartId, new ShoppingCartConfirmed(otherClientShoppingCartId, DateTime.UtcNow));

        // second confirmed
        eventStore.Append(otherConfirmedShoppingCartId, new ShoppingCartOpened(otherConfirmedShoppingCartId, clientId));
        eventStore.Append(otherConfirmedShoppingCartId, new ProductItemAddedToShoppingCart(otherConfirmedShoppingCartId, trousers));
        eventStore.Append(otherConfirmedShoppingCartId, new ShoppingCartConfirmed(otherConfirmedShoppingCartId, DateTime.UtcNow));

        // first pending
        eventStore.Append(otherPendingShoppingCartId, new ShoppingCartOpened(otherPendingShoppingCartId, clientId));

        // first confirmed
        var shoppingCart = database.Get<ShoppingCartDetails>(shoppingCartId)!;
        shoppingCart.Should().NotBeNull();
        shoppingCart.Id.Should().Be(shoppingCartId);
        shoppingCart.ClientId.Should().Be(clientId);
        shoppingCart.Status.Should().Be(ShoppingCartStatus.Confirmed);
        shoppingCart.ProductItems.Should().HaveCount(2);
        shoppingCart.ProductItems.Should().Contain(pairOfShoes);
        shoppingCart.ProductItems.Should().Contain(tShirt);

        var shoppingCartShortInfo = database.Get<ShoppingCartShortInfo>(shoppingCartId);
        shoppingCartShortInfo.Should().BeNull();

        // cancelled
        shoppingCart = database.Get<ShoppingCartDetails>(cancelledShoppingCartId)!;
        shoppingCart.Should().NotBeNull();
        shoppingCart.Id.Should().Be(cancelledShoppingCartId);
        shoppingCart.ClientId.Should().Be(clientId);
        shoppingCart.Status.Should().Be(ShoppingCartStatus.Canceled);
        shoppingCart.ProductItems.Should().HaveCount(1);
        shoppingCart.ProductItems.Should().Contain(dress);

        shoppingCartShortInfo = database.Get<ShoppingCartShortInfo>(cancelledShoppingCartId)!;
        shoppingCartShortInfo.Should().BeNull();

        // confirmed but other client
        shoppingCart = database.Get<ShoppingCartDetails>(otherClientShoppingCartId)!;
        shoppingCart.Should().NotBeNull();
        shoppingCart.Id.Should().Be(otherClientShoppingCartId);
        shoppingCart.ClientId.Should().Be(otherClientId);
        shoppingCart.Status.Should().Be(ShoppingCartStatus.Confirmed);
        shoppingCart.ProductItems.Should().HaveCount(1);
        shoppingCart.ProductItems.Should().Contain(dress);

        shoppingCartShortInfo = database.Get<ShoppingCartShortInfo>(otherClientShoppingCartId);
        shoppingCartShortInfo.Should().BeNull();

        // second confirmed
        shoppingCart = database.Get<ShoppingCartDetails>(otherConfirmedShoppingCartId)!;
        shoppingCart.Should().NotBeNull();
        shoppingCart.Id.Should().Be(otherConfirmedShoppingCartId);
        shoppingCart.ClientId.Should().Be(clientId);
        shoppingCart.Status.Should().Be(ShoppingCartStatus.Confirmed);
        shoppingCart.ProductItems.Should().HaveCount(1);
        shoppingCart.ProductItems.Should().Contain(trousers);

        shoppingCartShortInfo = database.Get<ShoppingCartShortInfo>(otherConfirmedShoppingCartId);
        shoppingCartShortInfo.Should().BeNull();

        // first pending
        shoppingCart = database.Get<ShoppingCartDetails>(otherPendingShoppingCartId)!;
        shoppingCart.Should().NotBeNull();
        shoppingCart.Id.Should().Be(otherPendingShoppingCartId);
        shoppingCart.ClientId.Should().Be(clientId);
        shoppingCart.Status.Should().Be(ShoppingCartStatus.Pending);
        shoppingCart.ProductItems.Should().BeEmpty();

        shoppingCartShortInfo = database.Get<ShoppingCartShortInfo>(otherPendingShoppingCartId)!;
        shoppingCartShortInfo.Should().NotBeNull();
        shoppingCartShortInfo.Id.Should().Be(otherPendingShoppingCartId);
        shoppingCartShortInfo.ClientId.Should().Be(clientId);
    }
}
