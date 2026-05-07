using Microsoft.AspNetCore.Mvc;
using GloboTicket.Catalog.Repositories;

namespace GloboTicket.Catalog.Controllers;

[ApiController]
[Route("[controller]")]
public class EventController : ControllerBase
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventController> _logger;

    public EventController(IEventRepository eventRepository, ILogger<EventController> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    [HttpGet(Name = "GetEvents")]
    public async Task<IEnumerable<Event>> GetAll()
    {
      return await _eventRepository.GetEvents();
    }

    [HttpGet("{id}", Name = "GetById")]
    public async Task<Event> GetById(Guid id)
    {
        return await _eventRepository.GetEventById(id);
    }

    public record ReservationRequest(int Count);

    // Reserve tickets for an event. Returns 409 if the event is sold out
    // or fewer tickets remain than were requested.
    [HttpPost("{id}/reserve", Name = "ReserveTickets")]
    public async Task<IActionResult> Reserve(Guid id, [FromBody] ReservationRequest request)
    {
        var success = await _eventRepository.ReserveTickets(id, request.Count);
        if (!success)
        {
            _logger.LogInformation("Reservation refused for {EventId}: {Count} unavailable", id, request.Count);
            return Conflict(new { message = "Not enough tickets available" });
        }
        _logger.LogInformation("Reserved {Count} tickets for {EventId}", request.Count, id);
        return Ok();
    }

    // Release a previously made reservation. Used as the compensating step
    // when a later stage of the checkout workflow fails.
    [HttpDelete("{id}/reserve", Name = "ReleaseTickets")]
    public async Task<IActionResult> Release(Guid id, [FromBody] ReservationRequest request)
    {
        await _eventRepository.ReleaseTickets(id, request.Count);
        _logger.LogInformation("Released {Count} tickets for {EventId}", request.Count, id);
        return Ok();
    }
}
