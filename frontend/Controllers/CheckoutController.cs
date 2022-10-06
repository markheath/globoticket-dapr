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
    private readonly Settings settings;
    private readonly ILogger<CheckoutController> logger;

    public CheckoutController(IShoppingBasketService shoppingBasketService,
        IOrderSubmissionService orderSubmissionService,
        Settings settings, ILogger<CheckoutController> logger)
    {
        this.shoppingBasketService = shoppingBasketService;
        this.orderSubmissionService = orderSubmissionService;
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

    public IActionResult Thanks()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Purchase(CheckoutViewModel checkout)
    {
        if (ModelState.IsValid)
        {
            var currentBasketId = Request.Cookies.GetCurrentBasketId(settings);
            checkout.BasketId = currentBasketId;

            logger.LogInformation($"Received an order from {checkout.Name}");
            var orderId = await orderSubmissionService.SubmitOrder(checkout);
            await shoppingBasketService.ClearBasket(currentBasketId);

            return RedirectToAction("Thanks");
        }
        else
        {
            return View("Index");
        }
    }

}
