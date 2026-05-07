using Dapr.Client;
using Dapr.Workflow;
using GloboTicket.Ordering.Services;
using GloboTicket.Ordering.Workflows;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers().AddDapr();
builder.Services.AddOpenApi();
builder.Services.AddTransient<EmailSender>();
// Pre-configured HttpClient routed through the local Dapr sidecar to the
// catalog app. Used by the reserve/release activities for service invocation.
builder.Services.AddSingleton(_ => DaprClient.CreateInvokeHttpClient(appId: "catalog"));
builder.Services.AddDaprWorkflow(options =>
{
    options.RegisterWorkflow<CheckoutWorkflow>();
    options.RegisterActivity<ReserveTicketsActivity>();
    options.RegisterActivity<ReleaseTicketsActivity>();
    options.RegisterActivity<ChargeCardActivity>();
    options.RegisterActivity<PersistOrderActivity>();
    options.RegisterActivity<SendEmailActivity>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCloudEvents();
app.MapDefaultEndpoints();
app.MapSubscribeHandler();
app.MapControllers();

app.Run();
