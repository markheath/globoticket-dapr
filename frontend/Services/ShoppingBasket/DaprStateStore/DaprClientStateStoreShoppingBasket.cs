using Dapr.Client;
using GloboTicket.Frontend.Models;
using GloboTicket.Frontend.Models.Api;


namespace GloboTicket.Frontend.Services;

public class DaprClientStateStoreShoppingBasket : IShoppingBasketService
{
    private readonly DaprClient daprClient;
    private readonly IEventCatalogService eventCatalogService;
    private readonly Settings settings;
    private readonly ILogger<DaprStateStoreShoppingBasket> logger;
    private const string stateStoreName = "shopstate";

    public DaprClientStateStoreShoppingBasket(
        DaprClient daprClient,
        IEventCatalogService eventCatalogService,
        Settings settings,
        ILogger<DaprStateStoreShoppingBasket> logger)
    {
        this.daprClient = daprClient;
        this.eventCatalogService = eventCatalogService;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task<BasketLine> AddToBasket(Guid basketId, BasketLineForCreation basketLineForCreation)
    {
        logger.LogInformation($"ADD TO BASKET {basketId}");
        var basket = await GetBasketFromStateStore(basketId);
        var @event = await GetEventFromStateStore(basketLineForCreation.EventId);

        var basketLine = new BasketLine()
        {
            EventId = basketLineForCreation.EventId,
            TicketAmount = basketLineForCreation.TicketAmount,
            Event = @event,
            BasketId = basket.BasketId,
            BasketLineId = Guid.NewGuid(),
            Price = basketLineForCreation.Price
        };
        basket.Lines.Add(basketLine);
        logger.LogInformation($"SAVING BASKET {basket.BasketId}");
        await SaveBasketToStateStore(basket);
        return basketLine;
    }

    public async Task<Basket> GetBasket(Guid basketId)
    {
        logger.LogInformation($"GET BASKET {basketId}");
        var basket = await GetBasketFromStateStore(basketId);

        return new Basket() { BasketId = basketId, NumberOfItems = basket.Lines.Count, UserId = basket.UserId };
    }

    public async Task<IEnumerable<BasketLine>> GetLinesForBasket(Guid basketId)
    {
        var basket = await GetBasketFromStateStore(basketId);
        return basket.Lines;
    }

    public async Task RemoveLine(Guid basketId, Guid lineId)
    {
        var basket = await GetBasketFromStateStore(basketId);
        var index = basket.Lines.FindIndex(bl => bl.BasketLineId == lineId);
        if (index >= 0) basket.Lines.RemoveAt(index);
        await SaveBasketToStateStore(basket);
    }
    public async Task UpdateLine(Guid basketId, BasketLineForUpdate basketLineForUpdate)
    {
        var basket = await GetBasketFromStateStore(basketId);
        var index = basket.Lines.FindIndex(bl => bl.BasketLineId == basketLineForUpdate.LineId);
        basket.Lines[index].TicketAmount = basketLineForUpdate.TicketAmount;
        await SaveBasketToStateStore(basket);
    }

    private async Task SaveBasketToStateStore(StateStoreBasket basket)
    {
        var key = $"basket-{basket.BasketId}";
        await daprClient.SaveStateAsync(stateStoreName, key, basket);
        logger.LogInformation($"Created new basket in state store {key}");
    }

    private async Task SaveEventToStateStore(Event @event)
    {
        var key = $"event-{@event.EventId}";
        logger.LogInformation($"Saving event to state store {key}");
        await daprClient.SaveStateAsync(stateStoreName, key, @event);
    }


    private async Task<StateStoreBasket> GetBasketFromStateStore(Guid basketId)
    {
        var key = $"basket-{basketId}";
        var basket = await daprClient.GetStateAsync<StateStoreBasket>(stateStoreName, key);
        if (basket == null)
        {
            if (basketId == Guid.Empty) basketId = Guid.NewGuid();
            logger.LogInformation($"CREATING NEW BASKET {basketId}");
            basket = new StateStoreBasket();
            basket.BasketId = basketId;
            basket.UserId = settings.UserId;
            basket.Lines = new List<BasketLine>();
            await SaveBasketToStateStore(basket);
        }
        return basket;
    }

    private async Task<Event> GetEventFromStateStore(Guid eventId)
    {
        var key = $"event-{eventId}";
        var @event = await daprClient.GetStateAsync<Event>(stateStoreName, key);

        if (@event != null)
        {
            logger.LogInformation("Using cached event");
        }
        else
        {
            @event = await eventCatalogService.GetEvent(eventId);
            await SaveEventToStateStore(@event);
        }
        return @event;
    }

    public async Task ClearBasket(Guid basketId)
    {
        var basket = await GetBasketFromStateStore(basketId);
        basket.Lines.Clear();
        await SaveBasketToStateStore(basket);
    }
}
