using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Commands;
using Sonarr.Api.V3.Series;

namespace NzbDrone.Host.WebHost.Series
{
    public class SeriesEditorModule: WebApiController
    {
        private readonly ISeriesService _seriesService;
        private readonly IManageCommandQueue _commandQueueManager;
        public SeriesEditorModule(ISeriesService seriesService, IManageCommandQueue commandQueueManager)
        {
            _seriesService = seriesService;
            _commandQueueManager = commandQueueManager;

            //Put("/",  series => SaveAll());
            //Delete("/",  series => DeleteSeries());
        }

        [Route(HttpVerbs.Put, "/")]
        public async Task<object> SaveAllAsync()
        {
            var bodyContent = await HttpContext.GetRequestBodyAsStringAsync();
            var resource = JsonSerializer.Deserialize<SeriesEditorResource>(bodyContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true});
            var seriesToUpdate = _seriesService.GetSeries(resource!.SeriesIds);
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

                if (!string.IsNullOrWhiteSpace(resource.RootFolderPath))
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

            HttpContext.Response.StatusCode = (int)HttpStatusCode.Accepted;
            return _seriesService.UpdateSeries(seriesToUpdate, !resource.MoveFiles)
                .ToResource();

            //return ResponseWithCode(_seriesService.UpdateSeries(seriesToUpdate, !resource.MoveFiles)
            //                     .ToResource()
            //                     , HttpStatusCode.Accepted);
        }

        [Route(HttpVerbs.Delete, "/")]
        public async Task<object> DeleteSeriesAsync()
        {
            var bodyContent = await HttpContext.GetRequestBodyAsStringAsync();
            var resource = JsonSerializer.Deserialize<SeriesEditorResource>(bodyContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true});

            foreach (var seriesId in resource!.SeriesIds)
            {
                throw new NotImplementedException("Testing!");
                //_seriesService.DeleteSeries(seriesId, resource.DeleteFiles, resource.AddImportListExclusion);
            }

            return Task.FromResult<object>(new object());
            return new object();/**/
        }
    }
}
