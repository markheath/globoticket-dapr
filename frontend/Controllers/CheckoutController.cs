using System.Net;
using GloboTicket.Frontend.Extensions;
using GloboTicket.Frontend.Models;
using GloboTicket.Frontend.Models.View;
using GloboTicket.Frontend.Services;
using GloboTicket.Frontend.Services.Ordering;
using Microsoft.AspNetCore.Mvc;

namespace GloboTicket.Frontend.Controllers;

public class CheckoutController : Controller
{
    private readonly IShoppingBasketService shoppingBasketService;
    private readonly IOrderSubmissionService orderSubmissionService;
    private readonly HttpClient orderingClient;
    private readonly Settings settings;
    private readonly ILogger<CheckoutController> logger;

    public CheckoutController(IShoppingBasketService shoppingBasketService,
        IOrderSubmissionService orderSubmissionService,
        HttpClient orderingClient,
        Settings settings, ILogger<CheckoutController> logger)
    {
        this.shoppingBasketService = shoppingBasketService;
        this.orderSubmissionService = orderSubmissionService;
        this.orderingClient = orderingClient;
        this.settings = settings;
        this.logger = logger;
    }

    public IActionResult Index()
    {
        var currentBasketId = Request.Cookies.GetCurrentBasketId(settings);

        // prefill to make demos easier
        var viewModel = new CheckoutViewModel() {
            BasketId = currentBasketId , Name = "A Customer",
            Address = "123 Example Street", Town = "Daprton",
            PostalCode = "DA1 2PR", Email = "customer@example.com",
            CreditCard = "4242424242424242", CreditCardDate = "10/26"
            };
        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Purchase(CheckoutViewModel checkout)
    {
        if (ModelState.IsValid)
        {
            var currentBasketId = Request.Cookies.GetCurrentBasketId(settings);
            checkout.BasketId = currentBasketId;

            logger.LogInformation("Received an order from {CustomerName}", checkout.Name);
            var orderId = await orderSubmissionService.SubmitOrder(checkout);
            await shoppingBasketService.ClearBasket(currentBasketId);

            return RedirectToAction(nameof(Order), new { orderId });
        }
        else
        {
            return View("Index");
        }
    }

    // Live status page for a submitted order. The page itself is static —
    // it polls OrderStatus below to render workflow progress in real time.
    public IActionResult Order(Guid orderId)
    {
        ViewData["OrderId"] = orderId;
        return View();
    }

    // JSON pass-through to the ordering service's workflow status endpoint.
    // Polled by the Order page; not consumed by any server-side code.
    [HttpGet]
    public async Task<IActionResult> OrderStatus(Guid orderId)
    {
        var response = await orderingClient.GetAsync($"order/{orderId}/status");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        var body = await response.Content.ReadAsStringAsync();
        return Content(body, "application/json");
    }
}
