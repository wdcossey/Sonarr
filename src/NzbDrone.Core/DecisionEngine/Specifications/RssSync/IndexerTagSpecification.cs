using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class IndexerTagSpecification : IDecisionEngineSpecification
    {
        private readonly ILogger<IndexerTagSpecification> _logger;
        private readonly IIndexerFactory _indexerFactory;

        public IndexerTagSpecification(ILogger<IndexerTagSpecification> logger, IIndexerFactory indexerFactory)
        {
            _logger = logger;
            _indexerFactory = indexerFactory;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (subject.Release == null || subject.Series?.Tags == null || subject.Release.IndexerId == 0)
            {
                return Decision.Accept();
            }

            IndexerDefinition indexer;
            try
            {
                indexer = _indexerFactory.Get(subject.Release.IndexerId);
            }
            catch (ModelNotFoundException)
            {
                _logger.LogDebug("Indexer with id {IndexerId} does not exist, skipping indexer tags check", subject.Release.IndexerId);
                return Decision.Accept();
            }

            // If indexer has tags, check that at least one of them is present on the series
            var indexerTags = indexer.Tags;

            if (indexerTags.Any() && indexerTags.Intersect(subject.Series.Tags).Empty())
            {
                _logger.LogDebug("Indexer {Indexer} has tags. None of these are present on series {Series}. Rejecting", subject.Release.Indexer, subject.Series);

                return Decision.Reject("Series tags do not match any of the indexer tags");
            }

            return Decision.Accept();
        }
    }
}
