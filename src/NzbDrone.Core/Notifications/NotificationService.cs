using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.Notifications
{
    public class NotificationService : IHandleAsync<EpisodeGrabbedEvent>,
                                       IHandleAsync<EpisodeImportedEvent>,
                                       IHandleAsync<SeriesRenamedEvent>,
                                       IHandleAsync<SeriesDeletedEvent>,
                                       IHandleAsync<EpisodeFileDeletedEvent>,
                                       IHandleAsync<HealthCheckFailedEvent>,
                                       IHandleAsync<DeleteCompletedEvent>,
                                       IHandleAsync<DownloadsProcessedEvent>,
                                       IHandleAsync<RenameCompletedEvent>,
                                       IHandleAsync<HealthCheckCompleteEvent>
    {
        private readonly INotificationFactory _notificationFactory;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(INotificationFactory notificationFactory, ILogger<NotificationService> logger)
        {
            _notificationFactory = notificationFactory;
            _logger = logger;
        }

        private string GetMessage(Series series, List<Episode> episodes, QualityModel quality)
        {
            var qualityString = quality.Quality.ToString();

            if (quality.Revision.Version > 1)
            {
                if (series.SeriesType == SeriesTypes.Anime)
                {
                    qualityString += " v" + quality.Revision.Version;
                }

                else
                {
                    qualityString += " Proper";
                }
            }

            if (series.SeriesType == SeriesTypes.Daily)
            {
                var episode = episodes.First();

                return string.Format("{0} - {1} - {2} [{3}]",
                                         series.Title,
                                         episode.AirDate,
                                         episode.Title,
                                         qualityString);
            }

            var episodeNumbers = string.Concat(episodes.Select(e => e.EpisodeNumber)
                                                       .Select(i => string.Format("x{0:00}", i)));

            var episodeTitles = string.Join(" + ", episodes.Select(e => e.Title));

            return string.Format("{0} - {1}{2} - {3} [{4}]",
                                    series.Title,
                                    episodes.First().SeasonNumber,
                                    episodeNumbers,
                                    episodeTitles,
                                    qualityString);
        }

        private bool ShouldHandleSeries(ProviderDefinition definition, Series series)
        {
            if (definition.Tags.Empty())
            {
                _logger.LogDebug("No tags set for this notification.");
                return true;
            }

            if (definition.Tags.Intersect(series.Tags).Any())
            {
                _logger.LogDebug("Notification and series have one or more intersecting tags.");
                return true;
            }

            _logger.LogDebug("{DefinitionName} does not have any intersecting tags with {SeriesTitle}. Notification will not be sent.", definition.Name, series.Title);
            return false;
        }

        private bool ShouldHandleHealthFailure(HealthCheck.HealthCheck healthCheck, bool includeWarnings)
        {
            if (healthCheck.Type == HealthCheckResult.Error)
            {
                return true;
            }

            if (healthCheck.Type == HealthCheckResult.Warning && includeWarnings)
            {
                return true;
            }

            return false;
        }

        public Task HandleAsync(EpisodeGrabbedEvent message)
        {
            var grabMessage = new GrabMessage
            {
                Message = GetMessage(message.Episode.Series, message.Episode.Episodes, message.Episode.ParsedEpisodeInfo.Quality),
                Series = message.Episode.Series,
                Quality = message.Episode.ParsedEpisodeInfo.Quality,
                Episode = message.Episode,
                DownloadClient = message.DownloadClient,
                DownloadId = message.DownloadId
            };

            foreach (var notification in _notificationFactory.OnGrabEnabled())
            {
                try
                {
                    if (!ShouldHandleSeries(notification.Definition, message.Episode.Series)) continue;
                    notification.OnGrab(grabMessage);
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to send OnGrab notification to {DefinitionName}", notification.Definition.Name);
                }
            }
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(EpisodeImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return Task.CompletedTask;
            }

            var downloadMessage = new DownloadMessage
            {
                Message = GetMessage(message.EpisodeInfo.Series, message.EpisodeInfo.Episodes, message.EpisodeInfo.Quality),
                Series = message.EpisodeInfo.Series,
                EpisodeFile = message.ImportedEpisode,
                OldFiles = message.OldFiles,
                SourcePath = message.EpisodeInfo.Path,
                DownloadClient = message.DownloadClientInfo?.Name,
                DownloadId = message.DownloadId
            };

            foreach (var notification in _notificationFactory.OnDownloadEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, message.EpisodeInfo.Series))
                    {
                        if (downloadMessage.OldFiles.Empty() || ((NotificationDefinition)notification.Definition).OnUpgrade)
                        {
                            notification.OnDownload(downloadMessage);
                        }
                    }
                }

                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to send OnDownload notification to: {DefinitionName}", notification.Definition.Name);
                }
            }
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(SeriesRenamedEvent message)
        {
            foreach (var notification in _notificationFactory.OnRenameEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, message.Series))
                    {
                        notification.OnRename(message.Series, message.RenamedFiles);
                    }
                }

                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to send OnRename notification to: {DefinitionName}", notification.Definition.Name);
                }
            }
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(EpisodeFileDeletedEvent message)
        {
            if (message.EpisodeFile.Episodes.Value.Empty())
            {
                _logger.LogTrace("Skipping notification for deleted file without an episode (episode metadata was removed)");
                return Task.CompletedTask;
            }

            var deleteMessage = new EpisodeDeleteMessage();
            deleteMessage.Message = GetMessage(message.EpisodeFile.Series, message.EpisodeFile.Episodes, message.EpisodeFile.Quality);
            deleteMessage.Series = message.EpisodeFile.Series;
            deleteMessage.EpisodeFile = message.EpisodeFile;
            deleteMessage.Reason = message.Reason;

            foreach (var notification in _notificationFactory.OnEpisodeFileDeleteEnabled())
            {
                try
                {
                    if (message.Reason != MediaFiles.DeleteMediaFileReason.Upgrade || ((NotificationDefinition)notification.Definition).OnEpisodeFileDeleteForUpgrade)
                    {
                        if (ShouldHandleSeries(notification.Definition, deleteMessage.EpisodeFile.Series))
                        {
                            notification.OnEpisodeFileDelete(deleteMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to send OnDelete notification to: {DefinitionName}", notification.Definition.Name);
                }
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(SeriesDeletedEvent message)
        {
            var deleteMessage = new SeriesDeleteMessage(message.Series,message.DeleteFiles);

            foreach (var notification in _notificationFactory.OnSeriesDeleteEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, deleteMessage.Series))
                    {
                        notification.OnSeriesDelete(deleteMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to send OnDelete notification to: {DefinitionName}", notification.Definition.Name);
                }
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(HealthCheckFailedEvent message)
        {
            foreach (var notification in _notificationFactory.OnHealthIssueEnabled())
            {
                try
                {
                    if (ShouldHandleHealthFailure(message.HealthCheck, ((NotificationDefinition)notification.Definition).IncludeHealthWarnings))
                    {
                        notification.OnHealthIssue(message.HealthCheck);
                    }
                }

                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to send OnHealthIssue notification to: {DefinitionName}", notification.Definition.Name);
                }
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(DeleteCompletedEvent message)
        {
            ProcessQueue();
            return Task.CompletedTask;
        }

        public Task HandleAsync(DownloadsProcessedEvent message)
        {
            ProcessQueue();
            return Task.CompletedTask;
        }

        public Task HandleAsync(RenameCompletedEvent message)
        {
            ProcessQueue();
            return Task.CompletedTask;
        }

        public Task HandleAsync(HealthCheckCompleteEvent message)
        {
            ProcessQueue();
            return Task.CompletedTask;
        }

        private void ProcessQueue()
        {
            foreach (var notification in _notificationFactory.GetAvailableProviders())
            {
                try
                {
                    notification.ProcessQueue();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to process notification queue for {DefinitionName}", notification.Definition.Name);
                }
            }
        }
    }
}
