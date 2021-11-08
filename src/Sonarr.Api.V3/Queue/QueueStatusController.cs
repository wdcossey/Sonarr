using Microsoft.AspNetCore.Mvc;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Queue
{
    [ApiController]
    [SonarrApiRoute("queue/status", RouteVersion.V3)]
    public class QueueStatusController : ControllerBase
    {
        private readonly IQueueStatusDebounceWrapper _debounceWrapper;

        public QueueStatusController(IQueueStatusDebounceWrapper debounceWrapper)
            => _debounceWrapper = debounceWrapper;
        
        [HttpGet]
        public IActionResult GetQueueStatusResponse()
            => Ok(_debounceWrapper.GetQueueStatus());
    }
}
