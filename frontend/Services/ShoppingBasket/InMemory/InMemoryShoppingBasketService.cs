using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GloboTicket.Frontend.Extensions;
using GloboTicket.Frontend.Models;
using GloboTicket.Frontend.Models.Api;

namespace GloboTicket.Frontend.Services
{
    // In memory implementation
    public class InMemoryShoppingBasketService: IShoppingBasketService
    {
        private readonly Dictionary<Guid, InMemoryBasket> baskets;
        private readonly Dictionary<Guid, Event> eventsCache; // shopping basket lines need to get event date and name
        private readonly Settings settings;
        private readonly IEventCatalogService eventCatalogService;

        public InMemoryShoppingBasketService(Settings settings, IEventCatalogService eventCatalogService)
        {
            this.settings = settings;
            this.eventCatalogService = eventCatalogService;
            this.baskets = new Dictionary<Guid, InMemoryBasket>();
            this.eventsCache = new Dictionary<Guid, Event>();
        }

        public async Task<BasketLine> AddToBasket(Guid basketId, BasketLineForCreation basketLine)
        {
            if (!baskets.TryGetValue(basketId, out var basket))
            {
                basket = new InMemoryBasket(settings.UserId);
                baskets.Add(basket.BasketId, basket);
            }
            if (!eventsCache.TryGetValue(basketLine.EventId, out var @event))
            {
                @event = await eventCatalogService.GetEvent(basketLine.EventId);
                eventsCache.Add(basketLine.EventId, @event);
            }

            return basket.Add(basketLine, @event);
        }

        public async Task<Basket?> GetBasket(Guid basketId)
        {
            if (!baskets.TryGetValue(basketId, out var basket))
            {
                return null;
            }
            return new Basket()
            {
                BasketId = basketId,
                NumberOfItems = basket.Lines.Count,
                UserId = basket.UserId
            };

        }

        public async Task<IEnumerable<BasketLine>> GetLinesForBasket(Guid basketId)
        {
            if (!baskets.TryGetValue(basketId, out var basket))
            {
                return new BasketLine[0];
            }
            return basket.Lines;
        }

        public async Task UpdateLine(Guid basketId, BasketLineForUpdate basketLineForUpdate)
        {
            if (baskets.TryGetValue(basketId, out var basket))
            {
                basket.Update(basketLineForUpdate);
            }
        }

        public async Task RemoveLine(Guid basketId, Guid lineId)
        {
            if (baskets.TryGetValue(basketId, out var basket))
            {
                basket.Remove(lineId);
            }
        }

        public Task ClearBasket(Guid basketId)
        {
            if (baskets.TryGetValue(basketId, out var basket))
            {
                basket.Clear();
            }
            return Task.CompletedTask;
        }
    }
}
