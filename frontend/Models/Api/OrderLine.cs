namespace GloboTicket.Frontend.Models.Api;

public class OrderLine
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public int TicketCount { get; set; }
    public int Price { get; set; }

}
