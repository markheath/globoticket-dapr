namespace GloboTicket.Catalog.Data;

internal static class SampleData
{
    private static readonly Guid JohnEgbertId = Guid.Parse("CFB88E29-4744-48C0-94FA-B25B92DEA317");
    private static readonly Guid NickSailorId = Guid.Parse("CFB88E29-4744-48C0-94FA-B25B92DEA318");
    private static readonly Guid MichaelJohnsonId = Guid.Parse("CFB88E29-4744-48C0-94FA-B25B92DEA319");
    private static readonly Guid AishaPatelId = Guid.Parse("CFB88E29-4744-48C0-94FA-B25B92DEA320");
    private static readonly Guid MayaOkaforId = Guid.Parse("CFB88E29-4744-48C0-94FA-B25B92DEA321");
    private static readonly Guid SunlightAvenueId = Guid.Parse("CFB88E29-4744-48C0-94FA-B25B92DEA322");
    private static readonly Guid LighthouseId = Guid.Parse("CFB88E29-4744-48C0-94FA-B25B92DEA323");
    private static readonly Guid LettersId = Guid.Parse("CFB88E29-4744-48C0-94FA-B25B92DEA324");

    private static readonly Dictionary<Guid, int> DefaultPrices = new()
    {
        [JohnEgbertId] = 65,
        [NickSailorId] = 135,
        [MichaelJohnsonId] = 85,
        [AishaPatelId] = 70,
        [MayaOkaforId] = 80,
        [SunlightAvenueId] = 120,
        [LighthouseId] = 55,
        [LettersId] = 60,
    };

    // Three inventory tiers so the workflow's reservation/compensation paths
    // can all be demonstrated: plenty, nearly sold out, and sold out.
    private static readonly Dictionary<Guid, int> DefaultStock = new()
    {
        [MichaelJohnsonId] = 100,
        [JohnEgbertId] = 3,
        [NickSailorId] = 0,
        [AishaPatelId] = 100,
        [MayaOkaforId] = 100,
        [SunlightAvenueId] = 100,
        [LighthouseId] = 7,
        [LettersId] = 100,
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
        new Event
        {
            EventId = AishaPatelId,
            Name = "An Evening with Aisha Patel",
            Price = DefaultPrices[AishaPatelId],
            Artist = "Aisha Patel",
            Date = DateTime.UtcNow.AddMonths(11),
            Description = "Aisha Patel returns to the city stage for one night only, blending classical violin with the rhythms of her South-Asian heritage. A warm, intimate evening that has sold out venues from London to Singapore.",
            ImageUrl = "/img/aisha.jpg",
            TicketsAvailable = DefaultStock[AishaPatelId],
        },
        new Event
        {
            EventId = MayaOkaforId,
            Name = "Midnight Sessions with Maya Okafor",
            Price = DefaultPrices[MayaOkaforId],
            Artist = "Maya Okafor",
            Date = DateTime.UtcNow.AddMonths(13),
            Description = "Three-time Grammy nominee Maya Okafor brings her signature blend of jazz, soul and contemporary R&B to the headline stage. Expect new material from her upcoming album alongside the songs you already love.",
            ImageUrl = "/img/maya.jpg",
            TicketsAvailable = DefaultStock[MayaOkaforId],
        },
        new Event
        {
            EventId = SunlightAvenueId,
            Name = "Sunlight Avenue",
            Price = DefaultPrices[SunlightAvenueId],
            Artist = "Priya Raman",
            Date = DateTime.UtcNow.AddMonths(15),
            Description = "A vibrant new musical from composer Priya Raman following four neighbours over one transformative summer. Critics have called it the freshest score Broadway has heard in years.",
            ImageUrl = "/img/sunlight.jpg",
            TicketsAvailable = DefaultStock[SunlightAvenueId],
        },
        new Event
        {
            EventId = LighthouseId,
            Name = "The Lighthouse Keeper's Daughter",
            Price = DefaultPrices[LighthouseId],
            Artist = "Helena Marsh",
            Date = DateTime.UtcNow.AddMonths(18),
            Description = "Helena Marsh's quietly devastating two-hander has won this year's Olivier Award for Best New Play. A lighthouse, a long-kept secret, and a daughter returning home after twenty years away.",
            ImageUrl = "/img/lighthouse.jpg",
            TicketsAvailable = DefaultStock[LighthouseId],
        },
        new Event
        {
            EventId = LettersId,
            Name = "Letters from the Border",
            Price = DefaultPrices[LettersId],
            Artist = "Kenji Tanaka",
            Date = DateTime.UtcNow.AddMonths(20),
            Description = "Kenji Tanaka's celebrated drama, translated into eleven languages, makes its long-awaited debut on the main stage. A correspondence between two strangers across a closed border, and what happens when the border finally opens.",
            ImageUrl = "/img/letters.jpg",
            TicketsAvailable = DefaultStock[LettersId],
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
