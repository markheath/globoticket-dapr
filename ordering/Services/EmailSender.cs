using GloboTicket.Ordering.Model;

namespace GloboTicket.Ordering.Services;

public class EmailSender
{
    private readonly ILogger<EmailSender> logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        this.logger = logger;
    }

    public async Task SendEmailForOrder(OrderForCreation order)
    {
        logger.LogInformation($"Received a new order for {order.CustomerDetails.Email}");
        logger.LogWarning("Not using Dapr so no email sent");
    }
}

