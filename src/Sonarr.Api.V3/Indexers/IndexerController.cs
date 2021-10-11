using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Indexers;

namespace Sonarr.Api.V3.Indexers
{
    [ApiController]
    [SonarrApiRoute("indexer", RouteVersion.V3)]
    public class IndexerController : ProviderControllerBase<IndexerResource, IIndexer, IndexerDefinition>
    {
        public static readonly IndexerResourceMapper ResourceMapper = new IndexerResourceMapper();

        public IndexerController(IndexerFactory indexerFactory)
            : base(indexerFactory, ResourceMapper)
        {
        }

        protected override void Validate(IndexerDefinition definition, bool includeWarnings)
        {
            if (!definition.Enable) return;
            base.Validate(definition, includeWarnings);
        }
    }
}