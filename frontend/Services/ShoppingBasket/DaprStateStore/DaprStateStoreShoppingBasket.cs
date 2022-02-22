using GloboTicket.Frontend.Models;
using GloboTicket.Frontend.Models.Api;
using System.Text.Json;

namespace GloboTicket.Frontend.Services;

public class DaprStateStoreShoppingBasket : IShoppingBasketService
{
    private readonly HttpClient httpClient;
    private readonly IEventCatalogService eventCatalogService;
    private readonly Settings settings;
    private readonly ILogger<DaprStateStoreShoppingBasket> logger;

    public DaprStateStoreShoppingBasket(HttpClient httpClient,
        IEventCatalogService eventCatalogService,
        Settings settings,
        ILogger<DaprStateStoreShoppingBasket> logger)
    {
        this.httpClient = httpClient;
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

        return new Basket() { 
            BasketId = basketId, 
            NumberOfItems = basket.Lines.Count, 
            UserId = basket.UserId };
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
        await SaveToStateStore(key, basket);
        logger.LogInformation($"Created new basket in state store {key}");
    }

    private async Task SaveToStateStore(string key, object value)
    {
        var resp = await httpClient.PostAsJsonAsync("", new[] { new { key, value = value } });
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            logger.LogError($"Failed to save state {key}: {body}");
        }
        resp.EnsureSuccessStatusCode();
    }

    private async Task SaveEventToStateStore(Event @event)
    {
        var key = $"event-{@event.EventId}";
        logger.LogInformation($"Saving event to state store {key}");
        await SaveToStateStore(key, @event);
    }

    private async Task<T?> GetFromStateStore<T>(string key)
    {
        var response = await httpClient.GetAsync(key);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(json))
        {
            var b = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            return b;
        }
        return default(T);
    }

    private async Task<StateStoreBasket> GetBasketFromStateStore(Guid basketId)
    {
        var key = $"basket-{basketId}";
        var basket = await GetFromStateStore<StateStoreBasket>(key);
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
        var @event = await GetFromStateStore<Event>(key);

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
        if (basket != null)
        {
            basket.Lines.Clear();
            await SaveBasketToStateStore(basket);
        }
    }
}
