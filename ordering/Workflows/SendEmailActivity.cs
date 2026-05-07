using Dapr.Workflow;
using GloboTicket.Ordering.Model;
using GloboTicket.Ordering.Services;

namespace GloboTicket.Ordering.Workflows;

public class SendEmailActivity : WorkflowActivity<OrderForCreation, object?>
{
    private readonly EmailSender emailSender;

    public SendEmailActivity(EmailSender emailSender)
    {
        this.emailSender = emailSender;
    }

    public override async Task<object?> RunAsync(WorkflowActivityContext context, OrderForCreation order)
    {
        await emailSender.SendEmailForOrder(order);
        return null;
    }
}
