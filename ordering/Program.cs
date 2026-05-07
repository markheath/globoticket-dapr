using GloboTicket.Ordering.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddDapr();
builder.Services.AddOpenApi();
builder.Services.AddTransient<EmailSender>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();

app.Run();
