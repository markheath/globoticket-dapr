using GloboTicket.Frontend.Models.Api;

namespace GloboTicket.Frontend.Models.View;

public class EventListModel
{
    public IEnumerable<Event> Events { get; set; } = new List<Event>();
    public int NumberOfItems { get; set; }
}
