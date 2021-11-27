namespace GloboTicket.Ordering.Model;

public class OrderLine
{
    public Guid EventId { get; set; }
    public int TicketCount { get; set; }
    public int Price { get; set; }

}
