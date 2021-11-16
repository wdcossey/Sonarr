using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Datastore;
using Sonarr.Http;
using Sonarr.Http.Attributes;
using Sonarr.Http.Extensions;
using Sonarr.Http.ModelBinders;

namespace Sonarr.Api.V3.Blocklist
{
    [ApiController]
    [SonarrApiRoute("blocklist", RouteVersion.V3)]
    public class BlocklistController : ControllerBase
    {
        private readonly BlocklistService _blocklistService;

        public BlocklistController(BlocklistService blocklistService)
            => _blocklistService = blocklistService;

        [ProducesResponseType(typeof(PagingResource<BlocklistResource>), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult Blocklist(
            [FromQuery] [ModelBinder(typeof(PagingResourceModelBinder))] PagingResource<BlocklistResource> pagingResource)
        {
            var pagingSpec = pagingResource.MapToPagingSpec<BlocklistResource, NzbDrone.Core.Blocklisting.Blocklist>("date", SortDirection.Descending);
            return Ok(pagingSpec.ApplyToPage(_blocklistService.Paged, BlocklistResourceMapper.MapToResource));
        }

        [ProducesResponseType(200)]
        [HttpDelete("{id:int}")]
        public IActionResult DeleteBlockList(int id)
        {
            _blocklistService.Delete(id);
            return Ok(new object());
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpDelete("bulk")]
        public IActionResult Remove([FromBody] BlocklistBulkResource resource)
        {
            _blocklistService.Delete(resource!.Ids);
            return Ok(new object());
        }

        /// <summary>
        /// Seems that the content-type is `application/x-www-form-urlencoded`, can't use `[FromBody]` :/
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpDelete("bulk")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult RemoveFromBody(
            [FromForm] [ModelBinder(typeof(FormEncodedBodyModelBinder))] BlocklistBulkResource resource)
            => Remove(resource);
    }
}
