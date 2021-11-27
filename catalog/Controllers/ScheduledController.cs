using Microsoft.AspNetCore.Mvc;

namespace GloboTicket.Catalog.Controllers
{
    [ApiController]
    [Route("scheduled")]
    public class ScheduledController : ControllerBase
    {
        private readonly ILogger<ScheduledController> _logger;

        public ScheduledController(ILogger<ScheduledController> logger)
        {
            _logger = logger;
        }
        [HttpPost("", Name = "Scheduled")]
        public void OnSchedule()
        {
            _logger.LogInformation("scheduled endpoint called");          
        }

        [HttpGet()]
        public string Test()
        {
            _logger.LogInformation("testing scheduled endpoint called");
            return "Working";
        }
    }
}
