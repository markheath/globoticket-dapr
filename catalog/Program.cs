using GloboTicket.Catalog.Data;
using GloboTicket.Catalog.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddControllers().AddDapr();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();
app.MapControllers();

await SeedAsync(app);

app.Run();

static async Task SeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();

    if (!await db.Events.AnyAsync())
    {
        db.Events.AddRange(SampleData.Events);
        await db.SaveChangesAsync();
    }
    else
    {
        // Mirror the cron tick: refresh seeded events back to their default
        // stock levels on every startup so the demo is self-healing across
        // long-running sessions.
        var events = await db.Events.ToListAsync();
        SampleData.ResetStock(events);
        await db.SaveChangesAsync();
    }
}
