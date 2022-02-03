using GloboTicket.Frontend.Models.Api;

namespace GloboTicket.Frontend.Services;

/// <summary>
///  Format in which we'll cache the basket in the Dapr state store
/// </summary>
public class StateStoreBasket
{
    public Guid BasketId { get; set; }
    public List<BasketLine> Lines { get; set; } = new List<BasketLine>();
    public Guid UserId { get; set; }
}
