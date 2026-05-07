using Dapr.Workflow;

namespace GloboTicket.Ordering.Workflows;

// Saga-style checkout. Each successful reservation is tracked so that any
// failure further down the pipeline can release every reservation made so
// far. After the card is charged the order is treated as committed and the
// remaining steps rely on activity retries rather than compensation.
public class CheckoutWorkflow : Workflow<CheckoutInput, CheckoutResult>
{
    public override async Task<CheckoutResult> RunAsync(WorkflowContext context, CheckoutInput input)
    {
        var order = input.Order;
        var reserved = new List<ReleaseRequest>();

        foreach (var line in order.Lines)
        {
            var ok = await context.CallActivityAsync<bool>(
                nameof(ReserveTicketsActivity),
                new ReserveRequest(line.EventId, line.TicketCount));

            if (!ok)
            {
                await ReleaseAll(context, reserved);
                return new CheckoutResult(false, $"Sold out: {line.EventName}");
            }
            reserved.Add(new ReleaseRequest(line.EventId, line.TicketCount));
        }

        var total = order.Lines.Sum(l => l.Price * l.TicketCount);
        var charged = await context.CallActivityAsync<bool>(
            nameof(ChargeCardActivity),
            new ChargeRequest(order.CustomerDetails.CreditCardNumber, total));

        if (!charged)
        {
            await ReleaseAll(context, reserved);
            return new CheckoutResult(false, "Card declined");
        }

        await context.CallActivityAsync(nameof(PersistOrderActivity), order);
        await context.CallActivityAsync(nameof(SendEmailActivity), order);

        return new CheckoutResult(true, order.OrderId.ToString());
    }

    private static async Task ReleaseAll(WorkflowContext context, List<ReleaseRequest> reserved)
    {
        foreach (var r in reserved)
        {
            await context.CallActivityAsync(nameof(ReleaseTicketsActivity), r);
        }
    }
}
