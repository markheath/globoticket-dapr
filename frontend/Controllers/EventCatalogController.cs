using GloboTicket.Frontend.Extensions;
using GloboTicket.Frontend.Models;
using GloboTicket.Frontend.Models.Api;
using GloboTicket.Frontend.Models.View;
using GloboTicket.Frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GloboTicket.Frontend.Controllers;

public class EventCatalogController : Controller
{
    private readonly IEventCatalogService eventCatalogService;
    private readonly IShoppingBasketService shoppingBasketService;
    private readonly Settings settings;
    private readonly ILogger<EventCatalogController> logger;

    public EventCatalogController(IEventCatalogService eventCatalogService, 
        IShoppingBasketService shoppingBasketService, 
        Settings settings,
        ILogger<EventCatalogController> logger)
    {
        this.eventCatalogService = eventCatalogService;
        this.shoppingBasketService = shoppingBasketService;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var currentBasketId = Request.Cookies.GetCurrentBasketId(settings);

        var getBasket = shoppingBasketService.GetBasket(currentBasketId);
        var getEvents = eventCatalogService.GetAll();
        var errorMessage = string.Empty;
        try
        {
            await Task.WhenAll(new Task[] { getBasket, getEvents });
        }
        catch(Exception ex)
        {
            // could be due an mDNS failure to talk to event catalog service when running locally
            // https://github.com/dapr/dapr/issues/3256
            logger.LogError(ex, "Failure fetching data for event catalog page");
            errorMessage = $"Unable to load data: {ex.Message}";
        }

        var numberOfItems = getBasket.IsCompletedSuccessfully ? getBasket.Result.NumberOfItems : 0;
        var events = getEvents.IsCompletedSuccessfully ? getEvents.Result : Array.Empty<Event>();
        

        return View(
            new EventListModel
            {
                Events = events,
                NumberOfItems = numberOfItems,
                ErrorMessage = errorMessage
            }
        );
    }

    public async Task<IActionResult> Detail(Guid eventId)
    {
        var ev = await eventCatalogService.GetEvent(eventId);
        return View(ev);
    }
}
