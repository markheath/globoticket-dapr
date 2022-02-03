using Dapr.Client;
using GloboTicket.Frontend.Models.Api;
using GloboTicket.Frontend.Models.View;

namespace GloboTicket.Frontend.Services.Ordering;

public class DaprOrderSubmissionService : IOrderSubmissionService
{
    private readonly IShoppingBasketService shoppingBasketService;
    private readonly DaprClient daprClient;
    private readonly ILogger<DaprOrderSubmissionService> logger;

    public DaprOrderSubmissionService(IShoppingBasketService shoppingBasketService, DaprClient daprClient, ILogger<DaprOrderSubmissionService> logger)
    {
        this.shoppingBasketService = shoppingBasketService;
        this.daprClient = daprClient;
        this.logger = logger;
    }
    public async Task<Guid> SubmitOrder(CheckoutViewModel checkoutViewModel)
    {
        var lines = await shoppingBasketService.GetLinesForBasket(checkoutViewModel.BasketId);
        var order = new OrderForCreation();
        order.Date = DateTimeOffset.Now;
        order.OrderId = Guid.NewGuid();
        order.Lines = lines.Select(line => new OrderLine() { 
            EventId = line.EventId, Price = line.Price, 
            TicketCount = line.TicketAmount }).ToList();
        order.CustomerDetails = new CustomerDetails()
        {
            Address = checkoutViewModel.Address,
            CreditCardNumber = checkoutViewModel.CreditCard,
            Email = checkoutViewModel.Email,
            Name = checkoutViewModel.Name,
            PostalCode = checkoutViewModel.PostalCode,
            Town = checkoutViewModel.Town,
            CreditCardExpiryDate = checkoutViewModel.CreditCardDate
        };
        logger.LogInformation("Posting order event to Dapr pubsub");
        await daprClient.PublishEventAsync("pubsub", "orders", order);
        return order.OrderId;
    }
}
