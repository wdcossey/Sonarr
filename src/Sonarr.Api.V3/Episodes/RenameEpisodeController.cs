﻿using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;

namespace Sonarr.Api.V3.Episodes
{
    [SonarrV3Route("episode/rename")]
    public class RenameEpisodeController : ControllerBase
    {
        private readonly IRenameEpisodeFileService _renameEpisodeFileService;

        public RenameEpisodeController(IRenameEpisodeFileService renameEpisodeFileService)
        {
            _renameEpisodeFileService = renameEpisodeFileService;
        }

        [HttpGet]
        public IActionResult GetEpisodes([FromQuery] int? seriesId, [FromQuery] int? seasonNumber)
        {

            if (!seriesId.HasValue)
                return BadRequest($"{nameof(seriesId)} is missing");

            return Ok(seasonNumber.HasValue
                ? _renameEpisodeFileService.GetRenamePreviews(seriesId.Value, seasonNumber.Value).ToResource()
                : _renameEpisodeFileService.GetRenamePreviews(seriesId.Value).ToResource());
        }
    }
}
