using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser;
using Sonarr.Api.V3.Episodes;
using Sonarr.Api.V3.Series;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Parse
{
    [ApiController]
    [SonarrApiRoute("parse", RouteVersion.V3)]
    public class ParseController : ControllerBase
    {
        private readonly IParsingService _parsingService;

        public ParseController(IParsingService parsingService)
            => _parsingService = parsingService;

        [HttpGet]
        public IActionResult Parse([FromQuery] string title, [FromQuery] string path)
        {
            if (path.IsNullOrWhiteSpace() && title.IsNullOrWhiteSpace())
                return BadRequest($"{nameof(title)} or {nameof(path)} is missing");

            var parsedEpisodeInfo = path.IsNotNullOrWhiteSpace() ? Parser.ParsePath(path) : Parser.ParseTitle(title);

            if (parsedEpisodeInfo == null)
            {
                return Ok(new ParseResource
                {
                    Title = title
                });
            }

            var remoteEpisode = _parsingService.Map(parsedEpisodeInfo, 0, 0);

            if (remoteEpisode != null)
            {
                return Ok(new ParseResource
                {
                    Title = title,
                    ParsedEpisodeInfo = remoteEpisode.ParsedEpisodeInfo,
                    Series = remoteEpisode.Series.ToResource(),
                    Episodes = remoteEpisode.Episodes.ToResource()
                });
            }

            return Ok(new ParseResource
            {
                Title = title,
                ParsedEpisodeInfo = parsedEpisodeInfo
            });
        }
    }
}