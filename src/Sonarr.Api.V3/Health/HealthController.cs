using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.HealthCheck;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Health
{
    [ApiController]
    [SonarrApiRoute("health", RouteVersion.V3)]
    public class HealthController : ControllerBase
    {
        private readonly IHealthCheckService _healthCheckService;

        public HealthController(IHealthCheckService healthCheckService)
            => _healthCheckService = healthCheckService;

        [HttpGet]
        public IActionResult GetHealth()
            => Ok(_healthCheckService.Results().ToResource());
    }
}
