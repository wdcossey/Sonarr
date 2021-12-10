using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using NzbDrone.Http.Ping;

namespace NzbDrone.Http
{
    [ApiController]
    [Route("ping")]
    public class PingController : ControllerBase
    {
        private readonly IConfigRepository _configRepository;

        public PingController(IConfigRepository configRepository)
        {
            _configRepository = configRepository;
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            try
            {
                _configRepository.All();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new PingResource {Status = "Error"});
            }

            return Ok(new PingResource {Status = "OK"});
        }
    }
}
