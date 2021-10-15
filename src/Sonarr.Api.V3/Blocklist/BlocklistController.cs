using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Datastore;
using Sonarr.Http;
using Sonarr.Http.Attributes;
using Sonarr.Http.Extensions;

namespace Sonarr.Api.V3.Blocklist
{
    [ApiController]
    [SonarrApiRoute("blocklist", RouteVersion.V3)]
    public class BlocklistController : ControllerBase
    {
        private readonly BlocklistService _blocklistService;

        public BlocklistController(BlocklistService blocklistService)
            => _blocklistService = blocklistService;

        [HttpGet]
        public IActionResult Blocklist([FromQuery] PagingResource<BlocklistResource> pagingResource)
        {
            var pagingSpec = pagingResource.MapToPagingSpec<BlocklistResource, NzbDrone.Core.Blocklisting.Blocklist>("date", SortDirection.Descending);
            return Ok(pagingSpec.ApplyToPage(_blocklistService.Paged, BlocklistResourceMapper.MapToResource));
        }

        [HttpDelete("{id:int}")]
        public IActionResult DeleteBlockList(int id)
        {
            _blocklistService.Delete(id);
            return Ok(new object());
        }

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
        [HttpDelete("bulk")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> RemoveFromBody()
        {
            using var reader = new StreamReader(Request.Body);
            var jsonContent = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(jsonContent))
                return BadRequest();

            //var resource = Request.Body.FromJson<BlocklistBulkResource>();
            var resource = Json.Deserialize<BlocklistBulkResource>(jsonContent);

            return Remove(resource);
        }
    }
}
