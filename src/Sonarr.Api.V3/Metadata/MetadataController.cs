using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Extras.Metadata;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Metadata
{
    [ApiController]
    [SonarrApiRoute("metadata", RouteVersion.V3)]
    public class MetadataController : ProviderControllerBase<MetadataResource, IMetadata, MetadataDefinition>
    {
        public static readonly MetadataResourceMapper ResourceMapper = new();

        public MetadataController(IMetadataFactory metadataFactory)
            : base(metadataFactory, ResourceMapper)
        {
        }

        protected override void Validate(MetadataDefinition definition, bool includeWarnings)
        {
            if (!definition.Enable) return;
            base.Validate(definition, includeWarnings);
        }
    }
}