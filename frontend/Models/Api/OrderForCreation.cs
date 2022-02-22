namespace GloboTicket.Frontend.Models.Api;

public class OrderForCreation
{
    public Guid OrderId { get; set; }
    public DateTimeOffset Date { get; set; }
    public CustomerDetails CustomerDetails { get; set; } = new CustomerDetails();
    public List<OrderLine> Lines { get; set; } = new List<OrderLine>();
}
