﻿using Microsoft.Extensions.Logging;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Specifications
{
    public class SameEpisodesImportSpecification : IImportDecisionEngineSpecification
    {
        private readonly SameEpisodesSpecification _sameEpisodesSpecification;
        private readonly ILogger<SameEpisodesImportSpecification> _logger;

        public SameEpisodesImportSpecification(SameEpisodesSpecification sameEpisodesSpecification, ILogger<SameEpisodesImportSpecification> logger)
        {
            _sameEpisodesSpecification = sameEpisodesSpecification;
            _logger = logger;
        }

        public RejectionType Type => RejectionType.Permanent;

        public Decision IsSatisfiedBy(LocalEpisode localEpisode, DownloadClientItem downloadClientItem)
        {
            if (_sameEpisodesSpecification.IsSatisfiedBy(localEpisode.Episodes))
            {
                return Decision.Accept();
            }

            _logger.LogDebug("Episode file on disk contains more episodes than this file contains");
            return Decision.Reject("Episode file on disk contains more episodes than this file contains");
        }
    }
}
