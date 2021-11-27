namespace GloboTicket.Catalog;
public class Event
{
    public Guid EventId { get; set; }
    public string Name { get; set; }
    public int Price { get; set; }
    public string Artist { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
}
