using System.Net.Http.Json;
using Dapr.Workflow;

namespace GloboTicket.Ordering.Workflows;

public class ReserveTicketsActivity : WorkflowActivity<ReserveRequest, bool>
{
    private readonly HttpClient catalogClient;
    private readonly ILogger<ReserveTicketsActivity> logger;

    public ReserveTicketsActivity(HttpClient catalogClient, ILogger<ReserveTicketsActivity> logger)
    {
        this.catalogClient = catalogClient;
        this.logger = logger;
    }

    public override async Task<bool> RunAsync(WorkflowActivityContext context, ReserveRequest input)
    {
        logger.LogInformation("Attempting to reserve {Count} tickets for {EventId}", input.Count, input.EventId);

        var response = await catalogClient.PostAsJsonAsync(
            $"event/{input.EventId}/reserve",
            new { count = input.Count });

        return response.IsSuccessStatusCode;
    }
}
