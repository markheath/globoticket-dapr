using GloboTicket.Catalog.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GloboTicket.Catalog.Controllers;

[ApiController]
[Route("scheduled")]
public class ScheduledController : ControllerBase
{
    private readonly ILogger<ScheduledController> logger;
    private IEventRepository eventRepository;

    public ScheduledController(ILogger<ScheduledController> logger,
        IEventRepository eventRepository)
    {
        this.logger = logger;
        this.eventRepository = eventRepository;
    }
    
    [HttpPost("", Name = "Scheduled")]
    public void OnSchedule()
    {
        var specialOfferEvent = eventRepository.UpdateSpecialOffer();
        var now = DateTime.Now.ToString("HH:mm:ss");
        logger.LogInformation($"{now} scheduled endpoint called: {specialOfferEvent.Name} is now on sale for {specialOfferEvent.Price}");
    }
}
