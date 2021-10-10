﻿using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Messaging.Events;

namespace Sonarr.Api.V3.Health
{
    [ApiController]
    [SonarrV3Route("health")]
    public class HealthController : ControllerBase, IHandle<HealthCheckCompleteEvent>
    {
        private readonly IHealthCheckService _healthCheckService;

        public HealthController(
            //IBroadcastSignalRMessage signalRBroadcaster,
            IHealthCheckService healthCheckService)
            //: base(signalRBroadcaster)
        {
            _healthCheckService = healthCheckService;
        }

        [HttpGet]
        public IActionResult GetHealth()
            => Ok(_healthCheckService.Results().ToResource());

        public void Handle(HealthCheckCompleteEvent message)
        {
            //TODO
            //BroadcastResourceChange(ModelAction.Sync);
        }
    }
}
