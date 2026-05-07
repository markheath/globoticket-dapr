namespace GloboTicket.Catalog.Data;

internal static class SampleData
{
    private static readonly Guid JohnEgbertId = Guid.Parse("CFB88E29-4744-48C0-94FA-B25B92DEA317");
    private static readonly Guid NickSailorId = Guid.Parse("CFB88E29-4744-48C0-94FA-B25B92DEA318");
    private static readonly Guid MichaelJohnsonId = Guid.Parse("CFB88E29-4744-48C0-94FA-B25B92DEA319");

    private static readonly Dictionary<Guid, int> DefaultPrices = new()
    {
        [JohnEgbertId] = 65,
        [NickSailorId] = 135,
        [MichaelJohnsonId] = 85,
    };

    // Three inventory tiers so the workflow's reservation/compensation paths
    // can all be demonstrated: plenty, nearly sold out, and sold out.
    private static readonly Dictionary<Guid, int> DefaultStock = new()
    {
        [MichaelJohnsonId] = 100,
        [JohnEgbertId] = 3,
        [NickSailorId] = 0,
    };

    public static IReadOnlyList<Event> Events =>
    [
        new Event
        {
            EventId = JohnEgbertId,
            Name = "John Egbert Live",
            Price = DefaultPrices[JohnEgbertId],
            Artist = "John Egbert",
            Date = DateTime.UtcNow.AddMonths(6),
            Description = "Join John for his farewell tour across 15 continents. John really needs no introduction since he has already mesmerized the world with his banjo.",
            ImageUrl = "/img/banjo.jpg",
            TicketsAvailable = DefaultStock[JohnEgbertId],
        },
        new Event
        {
            EventId = MichaelJohnsonId,
            Name = "The State of Affairs: Michael Live!",
            Price = DefaultPrices[MichaelJohnsonId],
            Artist = "Michael Johnson",
            Date = DateTime.UtcNow.AddMonths(9),
            Description = "Michael Johnson doesn't need an introduction. His 25 concerts across the globe last year were seen by thousands. Can we add you to the list?",
            ImageUrl = "/img/michael.jpg",
            TicketsAvailable = DefaultStock[MichaelJohnsonId],
        },
        new Event
        {
            EventId = NickSailorId,
            Name = "To the Moon and Back",
            Price = DefaultPrices[NickSailorId],
            Artist = "Nick Sailor",
            Date = DateTime.UtcNow.AddMonths(8),
            Description = "The critics are over the moon and so will you after you've watched this sing and dance extravaganza written by Nick Sailor, the man from 'My dad and sister'.",
            ImageUrl = "/img/musical.jpg",
            TicketsAvailable = DefaultStock[NickSailorId],
        },
    ];

    public static void ResetPrices(IEnumerable<Event> events)
    {
        foreach (var e in events)
        {
            if (DefaultPrices.TryGetValue(e.EventId, out var price))
            {
                e.Price = price;
            }
        }
    }

    public static void ResetStock(IEnumerable<Event> events)
    {
        foreach (var e in events)
        {
            if (DefaultStock.TryGetValue(e.EventId, out var stock))
            {
                e.TicketsAvailable = stock;
            }
        }
    }
}
