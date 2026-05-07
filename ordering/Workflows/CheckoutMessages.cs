using GloboTicket.Ordering.Model;

namespace GloboTicket.Ordering.Workflows;

public record CheckoutInput(OrderForCreation Order);

public record CheckoutResult(bool Success, string Reason);

public record ReserveRequest(Guid EventId, int Count);

public record ReleaseRequest(Guid EventId, int Count);

public record ChargeRequest(string CreditCardNumber, int Amount);
