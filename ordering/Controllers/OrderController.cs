using Dapr;
using GloboTicket.Ordering.Model;
using GloboTicket.Ordering.Services;
using Microsoft.AspNetCore.Mvc;

namespace GloboTicket.Ordering.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderRepository orderRepository;
    private readonly EmailSender emailSender;
    private readonly ILogger<OrderController> logger;

    public OrderController(
        OrderRepository orderRepository,
        EmailSender emailSender,
        ILogger<OrderController> logger)
    {
        this.orderRepository = orderRepository;
        this.emailSender = emailSender;
        this.logger = logger;
    }

    // Step 1: receive the order from the frontend and persist it. The Dapr
    // state-store outbox atomically publishes an "order-confirmed" event on
    // the pubsub component as part of the same transaction.
    [HttpPost("", Name = "SubmitOrder")]
    [Topic("pubsub", "orders")]
    public async Task<IActionResult> Submit(OrderForCreation order)
    {
        logger.LogInformation("Persisting new order from {CustomerName}", order.CustomerDetails.Name);
        await orderRepository.SaveAsync(order);
        return Ok();
    }

    // Step 2: outbox-published event triggers email send. The email only fires
    // if the persistence transaction in step 1 succeeded.
    [HttpPost("confirmed", Name = "OrderConfirmed")]
    [Topic("pubsub", "order-confirmed")]
    public async Task<IActionResult> Confirmed(OrderForCreation order)
    {
        logger.LogInformation("Order persisted, sending confirmation email to {Email}", order.CustomerDetails.Email);
        await emailSender.SendEmailForOrder(order);
        return Ok();
    }
}
