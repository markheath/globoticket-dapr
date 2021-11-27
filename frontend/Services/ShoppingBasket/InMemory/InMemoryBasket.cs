using GloboTicket.Frontend.Models.Api;

namespace GloboTicket.Frontend.Services
{
    class InMemoryBasket
    {
        public InMemoryBasket(Guid userId)
        {
            BasketId = Guid.NewGuid();
            Lines = new List<BasketLine>();
            UserId = userId;
        }
        public Guid BasketId { get; }
        public List<BasketLine> Lines { get; }
        public Guid UserId { get;  }

        public BasketLine Add(BasketLineForCreation line, Event @event)
        {
            var basketLine = new BasketLine() { 
                EventId = line.EventId, 
                TicketAmount = line.TicketAmount,
                Event = @event,
                BasketId = this.BasketId, 
                BasketLineId = Guid.NewGuid(), 
                Price = line.Price 
            };
            Lines.Add(basketLine);
            return basketLine;
        }
        public void Remove(Guid lineId)
        {
            var index = Lines.FindIndex(bl => bl.BasketLineId == lineId);
            if (index >= 0) Lines.RemoveAt(index);
        }

        public void Update(BasketLineForUpdate basketLineForUpdate)
        {
            var index = Lines.FindIndex(bl => bl.BasketLineId == basketLineForUpdate.LineId);
            Lines[index].TicketAmount = basketLineForUpdate.TicketAmount;
        }
        public void Clear()
        {
            Lines.Clear();
        }
    }
}
