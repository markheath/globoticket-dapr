using Dapr.Workflow;

namespace GloboTicket.Ordering.Workflows;

// Mocked card-charge step. Deterministic decline trigger: any PAN ending
// in "0000" is declined. This lets the course demonstrate the workflow's
// compensation path on demand without needing a real payment gateway.
public class ChargeCardActivity : WorkflowActivity<ChargeRequest, bool>
{
    private readonly ILogger<ChargeCardActivity> logger;

    public ChargeCardActivity(ILogger<ChargeCardActivity> logger)
    {
        this.logger = logger;
    }

    public override Task<bool> RunAsync(WorkflowActivityContext context, ChargeRequest input)
    {
        var pan = (input.CreditCardNumber ?? string.Empty).Trim();
        if (pan.EndsWith("0000"))
        {
            logger.LogWarning("Mock charge declined for amount {Amount}", input.Amount);
            return Task.FromResult(false);
        }

        logger.LogInformation("Mock charge approved for amount {Amount}", input.Amount);
        return Task.FromResult(true);
    }
}
