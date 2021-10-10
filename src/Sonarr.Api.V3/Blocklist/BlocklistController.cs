using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Datastore;
using Sonarr.Http;

namespace Sonarr.Api.V3.Blocklist
{
    [ApiController]
    [Route("/api/v3/blocklist")]
    public class BlocklistController : SonarrPagedController<BlocklistResource>
    {
        private readonly BlocklistService _blocklistService;

        public BlocklistController(BlocklistService blocklistService)
            => _blocklistService = blocklistService;

        [HttpGet]
        public PagingResource<BlocklistResource> Blocklist([FromQuery] PagingResource<BlocklistResource> pagingResource)
        {
            var pagingSpec = pagingResource.MapToPagingSpec<BlocklistResource, NzbDrone.Core.Blocklisting.Blocklist>("date", SortDirection.Descending);
            return ApplyToPage(_blocklistService.Paged, pagingSpec, BlocklistResourceMapper.MapToResource);
        }

        [HttpDelete("{id:int}")]
        public IActionResult DeleteBlockList(int id)
        {
            _blocklistService.Delete(id);
            return Ok(new object());
        }

        [HttpDelete("bulk")]
        public async Task<IActionResult> Remove()
        {
            //Seems that the content-type is `application/x-www-form-urlencoded`, can't use `[FromBody]` :/
            using var reader = new StreamReader(Request.Body);
            var jsonContent = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(jsonContent))
                return BadRequest();

            var resource = JsonSerializer.Deserialize<BlocklistBulkResource>(jsonContent, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            _blocklistService.Delete(resource!.Ids);
            return Ok(new object());
        }
    }
}
