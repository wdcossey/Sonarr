using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.Tv;
using NzbDrone.SignalR;
using Sonarr.Api.V3.Episodes;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Wanted
{
    [BroadcastName("WantedCutoff")]
    public class CutoffEventHandler : EpisodeEventHandler
    {
        public CutoffEventHandler(IHubContext<SonarrHub, ISonarrHub> hubContext, IEpisodeService episodeService) 
            : base(hubContext, episodeService) { }
    }
}
