using GloboTicket.Frontend.Services;
using GloboTicket.Frontend.Models;
using GloboTicket.Frontend.Services.Ordering;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// note: for this demo we're using the DAPR_HTTP_PORT environment variable to decide if we're using Dapr or not
builder.Services.AddHttpClient<IEventCatalogService, EventCatalogService>((sp, c) =>
    c.BaseAddress = new Uri(sp.GetService<IConfiguration>()["ApiConfigs:EventCatalog:Uri"]));
builder.Services.AddSingleton<IShoppingBasketService, InMemoryShoppingBasketService>();
builder.Services.AddHttpClient<IOrderSubmissionService, HttpOrderSubmissionService>((sp, c) =>
    c.BaseAddress = new Uri(sp.GetService<IConfiguration>()["ApiConfigs:Ordering:Uri"]));

builder.Services.AddSingleton<Settings>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Turning this off to simplify the running in Kubernetes demo
// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=EventCatalog}/{action=Index}/{id?}");

app.Run();
