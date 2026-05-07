using GloboTicket.Catalog.Data;
using Microsoft.EntityFrameworkCore;

namespace GloboTicket.Catalog.Repositories;

public class EventRepository : IEventRepository
{
    private readonly CatalogDbContext db;

    public EventRepository(CatalogDbContext db)
    {
        this.db = db;
    }

    public async Task<IEnumerable<Event>> GetEvents()
    {
        return await db.Events.AsNoTracking().ToListAsync();
    }

    public async Task<Event> GetEventById(Guid eventId)
    {
        var @event = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (@event == null)
        {
            throw new InvalidOperationException("Event not found");
        }
        return @event;
    }

    // Scheduled task calls this periodically to put one event on special offer.
    // Resets prices and stock first so they don't drift over time.
    public async Task<Event> UpdateSpecialOffer()
    {
        var events = await db.Events.ToListAsync();
        foreach (var e in events)
        {
            e.IsOnSpecialOffer = false;
        }
        SampleData.ResetPrices(events);
        SampleData.ResetStock(events);

        var specialOfferEvent = events[Random.Shared.Next(events.Count)];
        specialOfferEvent.Price = (int)(specialOfferEvent.Price * 0.8);
        specialOfferEvent.IsOnSpecialOffer = true;

        await db.SaveChangesAsync();
        return specialOfferEvent;
    }

    // Atomic decrement at the database level: the row is only updated if
    // enough stock exists. Returns false if the event is sold out or the
    // requested quantity exceeds availability.
    public async Task<bool> ReserveTickets(Guid eventId, int count)
    {
        var rowsAffected = await db.Events
            .Where(e => e.EventId == eventId && e.TicketsAvailable >= count)
            .ExecuteUpdateAsync(s => s.SetProperty(
                e => e.TicketsAvailable,
                e => e.TicketsAvailable - count));
        return rowsAffected > 0;
    }

    // Compensating action when a downstream workflow step fails.
    public async Task ReleaseTickets(Guid eventId, int count)
    {
        await db.Events
            .Where(e => e.EventId == eventId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                e => e.TicketsAvailable,
                e => e.TicketsAvailable + count));
    }
}
