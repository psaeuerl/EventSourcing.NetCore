using IntroductionToEventSourcing.BusinessLogic.Mutable;

namespace IntroductionToEventSourcing.BusinessLogic.Immutable;
using static ShoppingCartEvent;
using static ShoppingCartCommand;

public abstract record ShoppingCartCommand
{
    public record OpenShoppingCart(
        Guid ShoppingCartId,
        Guid ClientId
    ): ShoppingCartCommand;

    public record AddProductItemToShoppingCart(
        Guid ShoppingCartId,
        ProductItem ProductItem
    );

    public record RemoveProductItemFromShoppingCart(
        Guid ShoppingCartId,
        PricedProductItem ProductItem
    );

    public record ConfirmShoppingCart(
        Guid ShoppingCartId
    );

    public record CancelShoppingCart(
        Guid ShoppingCartId
    ): ShoppingCartCommand;

    private ShoppingCartCommand() {}
}

public static class ShoppingCartService
{
    public static ShoppingCartOpened Handle(OpenShoppingCart command)
    {
        var scEvent = new ShoppingCartOpened(command.ShoppingCartId, command.ClientId);
        return scEvent;
    }

    public static ProductItemAddedToShoppingCart Handle(
        IProductPriceCalculator priceCalculator,
        AddProductItemToShoppingCart command,
        ShoppingCart shoppingCart
    )
    {
        var pricedProductItem = priceCalculator.Calculate(command.ProductItem);
        return new ProductItemAddedToShoppingCart(shoppingCart.Id, pricedProductItem);
    }

    public static ProductItemRemovedFromShoppingCart Handle(
        RemoveProductItemFromShoppingCart command,
        ShoppingCart shoppingCart
    )
    {
        return new ProductItemRemovedFromShoppingCart(shoppingCart.Id, command.ProductItem);
    }

    public static ShoppingCartConfirmed Handle(ConfirmShoppingCart command, ShoppingCart shoppingCart)
    {
        return new ShoppingCartConfirmed(shoppingCart.Id, DateTime.Now);
    }

    public static ShoppingCartCanceled Handle(CancelShoppingCart command, ShoppingCart shoppingCart)
    {
        if (shoppingCart.Status == ShoppingCartStatus.Confirmed)
        {
            throw new InvalidOperationException("Cannot Cancel because Shopping cart already confirmed");
        }
        return new ShoppingCartCanceled(command.ShoppingCartId, DateTime.Now);
    }
}
