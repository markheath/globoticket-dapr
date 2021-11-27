namespace GloboTicket.Frontend.Models.Api;

public class OrderForCreation
{
    public Guid OrderId { get; set; }
    public DateTimeOffset Date { get; set; }
    public CustomerDetails CustomerDetails { get; set; }
    public List<OrderLine> Lines { get; set; }
}
