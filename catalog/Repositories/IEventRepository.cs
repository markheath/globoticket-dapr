namespace GloboTicket.Catalog.Repositories;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetEvents();
    Task<Event> GetEventById(Guid eventId);
    Task<Event> UpdateSpecialOffer();
    Task<bool> ReserveTickets(Guid eventId, int count);
    Task ReleaseTickets(Guid eventId, int count);
}
