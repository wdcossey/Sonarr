using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.HealthCheck;

namespace Sonarr.Api.V3.Health
{
    [ApiController]
    [Route("/api/v3/health")]
    public class HealthController : ControllerBase
    {
        private readonly IHealthCheckService _healthCheckService;

        public HealthController(
            //IBroadcastSignalRMessage signalRBroadcaster,
            IHealthCheckService healthCheckService)
            //: base(signalRBroadcaster)
        {
            _healthCheckService = healthCheckService;
            /*GetResourceAll = GetHealth;*/
        }

        [HttpGet]
        public List<HealthResource> GetHealth()
        {
            return _healthCheckService.Results().ToResource();
        }
    }
}
