namespace GloboTicket.Ordering.Model;

public class OrderForCreation
{
    public DateTimeOffset Date { get; set; }
    public CustomerDetails CustomerDetails { get; set; }
    public List<OrderLine> Lines { get; set; }
}
