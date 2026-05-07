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
        logger.LogInformation(
            "Scheduled endpoint called: {EventName} is now on sale for {Price}",
            specialOfferEvent.Name, specialOfferEvent.Price);
    }
}
