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
}
