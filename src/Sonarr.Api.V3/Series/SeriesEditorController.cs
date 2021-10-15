﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Commands;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Series
{
    [ApiController]
    [SonarrApiRoute("series/editor", RouteVersion.V3)]
    public class SeriesEditorController : ControllerBase // SonarrV3Module
    {

        private readonly ISeriesService _seriesService;
        private readonly IManageCommandQueue _commandQueueManager;

        public SeriesEditorController(ISeriesService seriesService, IManageCommandQueue commandQueueManager)
            //: base("/series/editor")
        {
            _seriesService = seriesService;
            _commandQueueManager = commandQueueManager;
            //Put("/", series => SaveAll());
            //Delete("/", series => DeleteSeries());
        }

        [HttpPut]
        public IActionResult SaveAll([FromBody] SeriesEditorResource resource)
        {
            //var resource = Request.Body.FromJson<SeriesEditorResource>();
            var seriesToUpdate = _seriesService.GetSeries(resource.SeriesIds);
            var seriesToMove = new List<BulkMoveSeries>();

            foreach (var series in seriesToUpdate)
            {
                if (resource.Monitored.HasValue)
                {
                    series.Monitored = resource.Monitored.Value;
                }

                if (resource.QualityProfileId.HasValue)
                {
                    series.QualityProfileId = resource.QualityProfileId.Value;
                }

                if (resource.LanguageProfileId.HasValue)
                {
                    series.LanguageProfileId = resource.LanguageProfileId.Value;
                }

                if (resource.SeriesType.HasValue)
                {
                    series.SeriesType = resource.SeriesType.Value;
                }

                if (resource.SeasonFolder.HasValue)
                {
                    series.SeasonFolder = resource.SeasonFolder.Value;
                }

                if (resource.RootFolderPath.IsNotNullOrWhiteSpace())
                {
                    series.RootFolderPath = resource.RootFolderPath;
                    seriesToMove.Add(new BulkMoveSeries
                    {
                        SeriesId = series.Id,
                        SourcePath = series.Path
                    });
                }

                if (resource.Tags != null)
                {
                    var newTags = resource.Tags;
                    var applyTags = resource.ApplyTags;

                    switch (applyTags)
                    {
                        case ApplyTags.Add:
                            newTags.ForEach(t => series.Tags.Add(t));
                            break;
                        case ApplyTags.Remove:
                            newTags.ForEach(t => series.Tags.Remove(t));
                            break;
                        case ApplyTags.Replace:
                            series.Tags = new HashSet<int>(newTags);
                            break;
                    }
                }
            }

            if (resource.MoveFiles && seriesToMove.Any())
            {
                _commandQueueManager.Push(new BulkMoveSeriesCommand
                {
                    DestinationRootFolder = resource.RootFolderPath,
                    Series = seriesToMove
                });
            }

            return Accepted(_seriesService.UpdateSeries(seriesToUpdate, !resource.MoveFiles).ToResource());
        }

        [HttpDelete]
        public IActionResult DeleteSeries([FromBody] SeriesEditorResource resource)
        {
            foreach (var seriesId in resource.SeriesIds)
                _seriesService.DeleteSeries(seriesId, resource.DeleteFiles, resource.AddImportListExclusion);
            return Ok(new object());
        }

        [HttpDelete]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult DeleteSeriesFromForm() //TODO: This should be from the Body?!?
        {
            var resource = JsonSerializer.Deserialize<SeriesEditorResource>(Request.Form.FirstOrDefault().Key,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return DeleteSeries(resource);
        }
    }
}