using Dapr.Client;
using Dapr.Workflow;
using GloboTicket.Ordering.Model;

namespace GloboTicket.Ordering.Workflows;

public class PersistOrderActivity : WorkflowActivity<OrderForCreation, object?>
{
    private const string StateStoreName = "orderstore";

    private readonly DaprClient daprClient;
    private readonly ILogger<PersistOrderActivity> logger;

    public PersistOrderActivity(DaprClient daprClient, ILogger<PersistOrderActivity> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public override async Task<object?> RunAsync(WorkflowActivityContext context, OrderForCreation order)
    {
        logger.LogInformation("Persisting order {OrderId} for {Customer}", order.OrderId, order.CustomerDetails.Name);
        await daprClient.SaveStateAsync(StateStoreName, $"order-{order.OrderId}", order);
        return null;
    }
}
