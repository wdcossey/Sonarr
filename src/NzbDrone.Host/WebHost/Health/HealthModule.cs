using System.Collections.Generic;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NzbDrone.Core.HealthCheck;
using NzbDrone.SignalR;
using Sonarr.Api.V3.Health;

namespace NzbDrone.Host.WebHost.Health
{
    public class HealthModule: WebApiController
    {
        private readonly IHealthCheckService _healthCheckService;

        public HealthModule(IBroadcastSignalRMessage signalRBroadcaster, IHealthCheckService healthCheckService)
            //: base(signalRBroadcaster)
        {
            _healthCheckService = healthCheckService;
        }

        [Route(HttpVerbs.Get, "/")]
        public Task<IList<HealthResource>> GetHealth()
        {
            var result = _healthCheckService.Results().ToResource();
            return Task.FromResult<IList<HealthResource>>(result);
        }
    }
}
