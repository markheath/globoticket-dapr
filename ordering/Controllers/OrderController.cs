using GloboTicket.Ordering.Model;
using GloboTicket.Ordering.Services;
using Microsoft.AspNetCore.Mvc;

namespace GloboTicket.Ordering.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> logger;
    private readonly EmailSender emailSender;

    public OrderController(ILogger<OrderController> logger, EmailSender emailSender)
    {
        this.logger = logger;
        this.emailSender = emailSender;
    }

    [HttpPost("", Name = "SubmitOrder")]
    public async Task<IActionResult> Submit(OrderForCreation order)
    {
        logger.LogInformation($"Received a new order from {order.CustomerDetails.Name}");
        await emailSender.SendEmailForOrder(order);
        return Ok();
    }
}
