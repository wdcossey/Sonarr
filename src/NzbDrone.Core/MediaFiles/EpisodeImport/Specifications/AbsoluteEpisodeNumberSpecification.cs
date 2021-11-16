using System;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Specifications
{
    public class AbsoluteEpisodeNumberSpecification : IImportDecisionEngineSpecification
    {
        private readonly IBuildFileNames _buildFileNames;
        private readonly ILogger<AbsoluteEpisodeNumberSpecification> _logger;

        public AbsoluteEpisodeNumberSpecification(IBuildFileNames buildFileNames, ILogger<AbsoluteEpisodeNumberSpecification> logger)
        {
            _buildFileNames = buildFileNames;
            _logger = logger;
        }
        public Decision IsSatisfiedBy(LocalEpisode localEpisode, DownloadClientItem downloadClientItem)
        {
            if (localEpisode.Series.SeriesType != SeriesTypes.Anime)
            {
                _logger.LogDebug("Series type is not Anime, skipping check");
                return Decision.Accept();
            }

            if (!_buildFileNames.RequiresAbsoluteEpisodeNumber(localEpisode.Series, localEpisode.Episodes))
            {
                _logger.LogDebug("File name format does not require absolute episode number, skipping check");
                return Decision.Accept();
            }

            foreach (var episode in localEpisode.Episodes)
            {
                var airDateUtc = episode.AirDateUtc;
                var absoluteEpisodeNumber = episode.AbsoluteEpisodeNumber;

                if (airDateUtc.HasValue && airDateUtc.Value.Before(DateTime.UtcNow.AddDays(-1)))
                {
                    _logger.LogDebug("Episode aired more than 1 day ago");
                    continue;
                }

                if (!absoluteEpisodeNumber.HasValue)
                {
                    _logger.LogDebug("Episode does not have an absolute episode number and recently aired");

                    return Decision.Reject("Episode does not have an absolute episode number and recently aired");
                }
            }

            return Decision.Accept();
        }
    }
}
