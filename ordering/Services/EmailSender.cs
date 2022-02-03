using Dapr.Client;
using GloboTicket.Ordering.Model;

namespace GloboTicket.Ordering.Services;

public class EmailSender
{
    private readonly DaprClient daprClient;
    private readonly ILogger<EmailSender> logger;

    public EmailSender(DaprClient daprClient, ILogger<EmailSender> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task SendEmailForOrder(OrderForCreation order)
    {
        logger.LogInformation($"Received a new order for {order.CustomerDetails.Email}");

        var daprEnabled = !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DAPR_HTTP_PORT"));
        if (!daprEnabled) 
        { 
            logger.LogWarning("Not using Dapr so no email sent");
            return;
        }
        
        logger.LogInformation($"Sending email");
        var metadata = new Dictionary<string, string>
        {
          ["emailFrom"] = "noreply@globoticket.shop",
          ["emailTo"] = order.CustomerDetails.Email,
          ["subject"] = $"Thank you for your order"
        };
        var body = $"<h2>Your order has been received</h2>"
            + "<p>Your tickets are on the way!</p>";
        await daprClient.InvokeBindingAsync("sendmail", "create", 
            body, metadata);        
    }
}