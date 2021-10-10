using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Extras.Metadata;

namespace Sonarr.Api.V3.Metadata
{
    [ApiController]
    [SonarrV3Route("metadata")]
    public class MetadataController : ProviderControllerBase<MetadataResource, IMetadata, MetadataDefinition>
    {
        public static readonly MetadataResourceMapper ResourceMapper = new MetadataResourceMapper();

        public MetadataController(IMetadataFactory metadataFactory)
            : base(metadataFactory/*, "metadata"*/, ResourceMapper)
        {
        }

        protected override void Validate(MetadataDefinition definition, bool includeWarnings)
        {
            if (!definition.Enable) return;
            base.Validate(definition, includeWarnings);
        }
    }
}