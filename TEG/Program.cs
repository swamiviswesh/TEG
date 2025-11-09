// Program.cs
using TEGApi;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:5173",
            "https://*.vercel.app",
            "https://*.azurestaticapps.net"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(_ => true); // For development
    });
});

// Register services
builder.Services.AddSingleton<IEventService, EventService>();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

// API Endpoints
app.MapGet("/api/events", async (IEventService eventService) =>
{
    try
    {
        var events = await eventService.GetEventsAsync();
        return Results.Ok(events);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 503,
            title: "Service Unavailable"
        );
    }
})
.WithName("GetEvents")
.WithOpenApi();

app.MapGet("/api/venues", async (IEventService eventService) =>
{
    try
    {
        var venues = await eventService.GetVenuesAsync();
        return Results.Ok(venues);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 503,
            title: "Service Unavailable"
        );
    }
})
.WithName("GetVenues")
.WithOpenApi();

app.MapGet("/api/events/venue/{venueId}", async (int venueId, IEventService eventService) =>
{
    try
    {
        var events = await eventService.GetEventsByVenueIdAsync(venueId);
        return Results.Ok(events);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 503,
            title: "Service Unavailable"
        );
    }
})
.WithName("GetEventsByVenue")
.WithOpenApi();

app.MapGet("/api/events/enriched", async (IEventService eventService) =>
{
    try
    {
        var enrichedEvents = await eventService.GetEnrichedEventsAsync();
        return Results.Ok(enrichedEvents);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 503,
            title: "Service Unavailable"
        );
    }
})
.WithName("GetEnrichedEvents")
.WithOpenApi();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();

// Service Interface

// Service Implementation
