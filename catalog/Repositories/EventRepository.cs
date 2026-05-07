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
    // Resets all events first so prices and the on-sale flag don't drift over time.
    public async Task<Event> UpdateSpecialOffer()
    {
        var events = await db.Events.ToListAsync();
        foreach (var e in events)
        {
            e.IsOnSpecialOffer = false;
        }
        SampleData.ResetPrices(events);

        var specialOfferEvent = events[Random.Shared.Next(events.Count)];
        specialOfferEvent.Price = (int)(specialOfferEvent.Price * 0.8);
        specialOfferEvent.IsOnSpecialOffer = true;

        await db.SaveChangesAsync();
        return specialOfferEvent;
    }
}
