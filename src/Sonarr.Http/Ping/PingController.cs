using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using NzbDrone.Http.Ping;
using Sonarr.Http.Attributes;

namespace NzbDrone.Http
{
    [SonarrApiRoute("ping", RouteVersion.V3)]
    public class PingController : ControllerBase
    {
        private readonly IConfigRepository _configRepository;

        public PingController(IConfigRepository configRepository)
            => _configRepository = configRepository;

        [ProducesResponseType(typeof(PingResource), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PingResource), StatusCodes.Status500InternalServerError)]
        [HttpGet]
        public IActionResult GetStatus()
        {
            try
            {
                _configRepository.All();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new PingResource { Status = "Error" });
            }

            return Ok(new PingResource { Status = "OK" });
        }
    }
}
