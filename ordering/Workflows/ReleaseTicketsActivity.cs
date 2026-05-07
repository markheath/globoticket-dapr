using System.Net.Http.Json;
using Dapr.Workflow;

namespace GloboTicket.Ordering.Workflows;

public class ReleaseTicketsActivity : WorkflowActivity<ReleaseRequest, object?>
{
    private readonly HttpClient catalogClient;
    private readonly ILogger<ReleaseTicketsActivity> logger;

    public ReleaseTicketsActivity(HttpClient catalogClient, ILogger<ReleaseTicketsActivity> logger)
    {
        this.catalogClient = catalogClient;
        this.logger = logger;
    }

    public override async Task<object?> RunAsync(WorkflowActivityContext context, ReleaseRequest input)
    {
        logger.LogInformation("Releasing {Count} tickets for {EventId}", input.Count, input.EventId);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"event/{input.EventId}/reserve")
        {
            Content = JsonContent.Create(new { count = input.Count })
        };
        await catalogClient.SendAsync(request);
        return null;
    }
}
