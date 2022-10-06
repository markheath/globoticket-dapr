using System.Text;
using Dapr.Client;
using GloboTicket.Ordering.Model;

namespace GloboTicket.Ordering.Services;

public class EmailSender
{
    private readonly DaprClient daprClient;
    private readonly HttpClient catalogClient;
    private readonly ILogger<EmailSender> logger;

    public EmailSender(DaprClient daprClient, ILogger<EmailSender> logger, HttpClient catalogClient)
    {
        this.daprClient = daprClient;
        this.catalogClient = catalogClient;
        this.logger = logger;
    }

    private async Task<Event> GetEventDetails(Guid id) 
    {
        var eventDetails = await catalogClient.GetFromJsonAsync<Event>($"event/{id}");
        if (eventDetails == null) throw new InvalidOperationException("Failed to get event details");
        return eventDetails;
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
        var body = new StringBuilder();
        body.AppendLine($"<h2>Your order has been received</h2><ul>");
        foreach(var line in order.Lines)
        {
            var details = await GetEventDetails(line.EventId);
            body.AppendLine($"<li>{details.Name} ({details.Artist}) ${line.Price} Number of tickets:{line.TicketCount}</h2>");
        }
        body.AppendLine($"</ul>");
        body.AppendLine($"Total: ${order.Lines.Select(l => l.TicketCount * l.Price).Sum()}");
        body.AppendLine("<p>Your tickets are on the way! They will be delivered to:</p>");
        body.AppendLine($"{order.CustomerDetails.Name}<br/>{order.CustomerDetails.Address}<br/>");
        body.AppendLine($"{order.CustomerDetails.Town}<br/>{order.CustomerDetails.PostalCode}<br/>");
        
        await daprClient.InvokeBindingAsync("sendmail", "create", 
            body.ToString(), metadata);        
    }
}