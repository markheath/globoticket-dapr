using Dapr;
using Dapr.Workflow;
using GloboTicket.Ordering.Model;
using GloboTicket.Ordering.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace GloboTicket.Ordering.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly DaprWorkflowClient workflowClient;
    private readonly ILogger<OrderController> logger;

    public OrderController(DaprWorkflowClient workflowClient, ILogger<OrderController> logger)
    {
        this.workflowClient = workflowClient;
        this.logger = logger;
    }

    // The pubsub trigger is preserved as the entry point for the order
    // pipeline so the demo continues to teach pub/sub. The handler now
    // hands off to a Dapr Workflow which owns the saga end-to-end:
    // reserve → charge → persist → email, with compensating release of
    // any reserved tickets when an earlier stage fails.
    [HttpPost("", Name = "SubmitOrder")]
    [Topic("pubsub", "orders")]
    public async Task<IActionResult> Submit(OrderForCreation order)
    {
        var instanceId = order.OrderId.ToString();
        logger.LogInformation("Starting checkout workflow {InstanceId} for {Customer}",
            instanceId, order.CustomerDetails.Name);

        await workflowClient.ScheduleNewWorkflowAsync(
            name: nameof(CheckoutWorkflow),
            instanceId: instanceId,
            input: new CheckoutInput(order));

        return Accepted();
    }

    // Convenience endpoint for poking at workflow state from the Aspire
    // dashboard or a .http file. Not used by the frontend.
    [HttpGet("{orderId}/status", Name = "OrderStatus")]
    public async Task<IActionResult> Status(Guid orderId)
    {
        var state = await workflowClient.GetWorkflowStateAsync(orderId.ToString());
        if (state is null || !state.Exists)
        {
            return NotFound();
        }
        return Ok(new
        {
            RuntimeStatus = state.RuntimeStatus.ToString(),
            state.CreatedAt,
            state.LastUpdatedAt,
            CustomStatus = state.ReadCustomStatusAs<string>(),
            Output = state.ReadOutputAs<CheckoutResult>(),
        });
    }
}
