using GloboTicket.Frontend.Models;
using GloboTicket.Frontend.Services;
using GloboTicket.Frontend.Services.Ordering;
using Dapr.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDaprClient();

builder.Services.AddSingleton<IEventCatalogService>(sp =>
    new EventCatalogService(DaprClient.CreateInvokeHttpClient("catalog")));
builder.Services.AddScoped<IShoppingBasketService, DaprClientStateStoreShoppingBasket>();
builder.Services.AddScoped<IOrderSubmissionService, DaprOrderSubmissionService>();

builder.Services.AddSingleton<Settings>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=EventCatalog}/{action=Index}/{id?}");

app.Run();
