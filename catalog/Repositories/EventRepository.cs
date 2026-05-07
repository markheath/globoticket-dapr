using Dapr.Client;
using GloboTicket.Catalog.Data;
using Microsoft.EntityFrameworkCore;

namespace GloboTicket.Catalog.Repositories;

public class EventRepository : IEventRepository
{
    private readonly CatalogDbContext db;
    private readonly DaprClient daprClient;
    private readonly ILogger<EventRepository> logger;

    public EventRepository(CatalogDbContext db, DaprClient daprClient, ILogger<EventRepository> logger)
    {
        this.db = db;
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task<IEnumerable<Event>> GetEvents()
    {
        try
        {
            var connectionString = await GetConnectionStringFromSecretStore();
            logger.LogInformation("Connection string from Dapr secret store: {ConnectionString}", connectionString);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to fetch the connection string from the Dapr secret store");
        }

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

    // Demonstrates the Dapr secret store building block. The same code reads
    // from the local-file secret store in dev and from the Kubernetes secret
    // store in prod (controlled by the SECRET_STORE_NAME env var).
    private async Task<string> GetConnectionStringFromSecretStore()
    {
        var secretStoreName = Environment.GetEnvironmentVariable("SECRET_STORE_NAME") ?? "secretstore";
        var secretName = "eventcatalogdb";
        var secret = await daprClient.GetSecretAsync(secretStoreName, secretName);
        return secret[secretName];
    }
}
