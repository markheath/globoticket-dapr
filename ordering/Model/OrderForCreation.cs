namespace GloboTicket.Ordering.Model;

public record OrderForCreation(
    Guid OrderId,
    DateTimeOffset Date,
    CustomerDetails CustomerDetails,
    IEnumerable<OrderLine> Lines);
