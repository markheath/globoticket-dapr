using GloboTicket.Ordering.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers().AddDapr();
builder.Services.AddOpenApi();
builder.Services.AddTransient<EmailSender>();
builder.Services.AddTransient<OrderRepository>();

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
