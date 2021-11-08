using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.Tv;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.Series
{
    public class SeriesEventHandler : EventHandlerBase<SeriesResource, NzbDrone.Core.Tv.Series>
    {
        private readonly ISeriesService _seriesService;

        public SeriesEventHandler(IHubContext<SonarrHub, ISonarrHub> hubContext, ISeriesService seriesService) 
            : base(hubContext) => _seriesService = seriesService;
        
        protected override SeriesResource GetResourceById(int id)
            => _seriesService.GetSeries(id).ToResource(false);
        
    }
}
