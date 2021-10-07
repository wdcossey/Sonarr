﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.SeriesStats;
using Sonarr.Api.V3.Series;

namespace Sonarr.Api.V3.Series
{
    [ApiController]
    [Route("/api/v3/series/lookup")]
    public class SeriesLookupController : ControllerBase //SonarrRestModule<SeriesResource>
    {
        private readonly ISearchForNewSeries _searchProxy;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IMapCoversToLocal _coverMapper;

        public SeriesLookupController(
                ISearchForNewSeries searchProxy,
                IBuildFileNames fileNameBuilder,
                IMapCoversToLocal coverMapper)
            //: base("/series/lookup")
        {
            _searchProxy = searchProxy;
            _fileNameBuilder = fileNameBuilder;
            _coverMapper = coverMapper;
            //Get("/",  x => Search());
        }

        [HttpGet]
        public IEnumerable<SeriesResource> Search([FromQuery] string term)
        {
            var tvDbResults = _searchProxy.SearchForNewSeries(term);
            return MapToResource(tvDbResults);
        }

        private IEnumerable<SeriesResource> MapToResource(IEnumerable<NzbDrone.Core.Tv.Series> series)
        {
            foreach (var currentSeries in series)
            {
                var resource = currentSeries.ToResource();

                _coverMapper.ConvertToLocalUrls(resource.Id, resource.Images);

                var poster = currentSeries.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);

                if (poster != null)
                {
                    resource.RemotePoster = poster.RemoteUrl;
                }

                resource.Folder = _fileNameBuilder.GetSeriesFolder(currentSeries);
                resource.Statistics = new SeriesStatistics().ToResource(resource.Seasons);

                yield return resource;
            }
        }
    }
}