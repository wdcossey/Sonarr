using Microsoft.Extensions.Logging;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class TorrentSeedingSpecification : IDecisionEngineSpecification
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly ILogger<TorrentSeedingSpecification> _logger;

        public TorrentSeedingSpecification(IIndexerFactory indexerFactory, ILogger<TorrentSeedingSpecification> logger)
        {
            _indexerFactory = indexerFactory;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;


        public Decision IsSatisfiedBy(RemoteEpisode remoteEpisode, SearchCriteriaBase searchCriteria)
        {
            var torrentInfo = remoteEpisode.Release as TorrentInfo;

            if (torrentInfo == null || torrentInfo.IndexerId == 0)
            {
                return Decision.Accept();
            }

            IndexerDefinition indexer;
            try
            {
                indexer = _indexerFactory.Get(torrentInfo.IndexerId);
            }
            catch (ModelNotFoundException)
            {
                _logger.LogDebug("Indexer with id {IndexerId} does not exist, skipping seeders check", torrentInfo.IndexerId);
                return Decision.Accept();
            }

            if (indexer.Settings is ITorrentIndexerSettings torrentIndexerSettings)
            {
                var minimumSeeders = torrentIndexerSettings.MinimumSeeders;

                if (torrentInfo.Seeders.HasValue && torrentInfo.Seeders.Value < minimumSeeders)
                {
                    _logger.LogDebug("Not enough seeders: {Seeders}. Minimum seeders: {MinimumSeeders}", torrentInfo.Seeders, minimumSeeders);
                    return Decision.Reject("Not enough seeders: {0}. Minimum seeders: {1}", torrentInfo.Seeders, minimumSeeders);
                }
            }

            return Decision.Accept();
        }
    }
}
