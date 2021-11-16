﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Validation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Download.Clients.rTorrent;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download.Clients.RTorrent
{
    public class RTorrent : TorrentClientBase<RTorrentSettings>
    {
        private readonly IRTorrentProxy _proxy;
        private readonly IRTorrentDirectoryValidator _rTorrentDirectoryValidator;
        private readonly IDownloadSeedConfigProvider _downloadSeedConfigProvider;
        private readonly string _imported_view = String.Concat(BuildInfo.AppName.ToLower(), "_imported");

        public RTorrent(IRTorrentProxy proxy,
                        ITorrentFileInfoReader torrentFileInfoReader,
                        IHttpClient<RTorrent> httpClient,
                        IConfigService configService,
                        IDiskProvider diskProvider,
                        IRemotePathMappingService remotePathMappingService,
                        IDownloadSeedConfigProvider downloadSeedConfigProvider,
                        IRTorrentDirectoryValidator rTorrentDirectoryValidator,
                        ILogger<RTorrent> logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, remotePathMappingService, logger)
        {
            _proxy = proxy;
            _rTorrentDirectoryValidator = rTorrentDirectoryValidator;
            _downloadSeedConfigProvider = downloadSeedConfigProvider;
        }

        public override void MarkItemAsImported(DownloadClientItem downloadClientItem)
        {
            // Set post-import label
            if (Settings.TvImportedCategory.IsNotNullOrWhiteSpace() &&
                Settings.TvImportedCategory != Settings.TvCategory)
            {
                try
                {
                    _proxy.SetTorrentLabel(downloadClientItem.DownloadId.ToLower(), Settings.TvImportedCategory, Settings);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set torrent post-import label \"{TvImportedCategory}\" for {Title} in rTorrent. Does the label exist?",
                        Settings.TvImportedCategory, downloadClientItem.Title);
                }
            }

            // Set post-import view
            try
            {
                _proxy.PushTorrentUniqueView(downloadClientItem.DownloadId.ToLower(), _imported_view, Settings);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set torrent post-import view \"{ImportedView}\" for {Title} in rTorrent.",
                    _imported_view, downloadClientItem.Title);
            }
        }

        protected override string AddFromMagnetLink(RemoteEpisode remoteEpisode, string hash, string magnetLink)
        {
            var priority = (RTorrentPriority)(remoteEpisode.IsRecentEpisode() ? Settings.RecentTvPriority : Settings.OlderTvPriority);

            _proxy.AddTorrentFromUrl(magnetLink, Settings.TvCategory, priority, Settings.TvDirectory, Settings);

            var tries = 10;
            var retryDelay = 500;

            // Wait a bit for the magnet to be resolved.
            if (!WaitForTorrent(hash, tries, retryDelay))
            {
                _logger.LogWarning("rTorrent could not resolve magnet within {SecondsValue} seconds, download may remain stuck: {MagnetLink}.", tries * retryDelay / 1000, magnetLink);

                return hash;
            }

            return hash;
        }

        protected override string AddFromTorrentFile(RemoteEpisode remoteEpisode, string hash, string filename, byte[] fileContent)
        {
            var priority = (RTorrentPriority)(remoteEpisode.IsRecentEpisode() ? Settings.RecentTvPriority : Settings.OlderTvPriority);

            _proxy.AddTorrentFromFile(filename, fileContent, Settings.TvCategory, priority, Settings.TvDirectory, Settings);

            var tries = 10;
            var retryDelay = 500;
            if (!WaitForTorrent(hash, tries, retryDelay))
            {
                _logger.LogDebug("rTorrent didn't add the torrent within {SecondsValue} seconds: {Filename}.", tries * retryDelay / 1000, filename);

                throw new ReleaseDownloadException(remoteEpisode.Release, "Downloading torrent failed");
            }

            return hash;
        }

        public override string Name => "rTorrent";

        public override ProviderMessage Message => new ProviderMessage($"Sonarr will handle automatic removal of torrents based on the current seed criteria in Settings->Indexers. After importing it will also set \"{_imported_view}\" as an rTorrent view, which can be used in rTorrent scripts to customize behavior.", ProviderMessageType.Info);

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            var torrents = _proxy.GetTorrents(Settings);

            _logger.LogDebug("Retrieved metadata of {Count} torrents in client", torrents.Count);

            var items = new List<DownloadClientItem>();
            foreach (RTorrentTorrent torrent in torrents)
            {
                // Don't concern ourselves with categories other than specified
                if (Settings.TvCategory.IsNotNullOrWhiteSpace() && torrent.Category != Settings.TvCategory) continue;

                if (torrent.Path.StartsWith("."))
                {
                    throw new DownloadClientException("Download paths must be absolute. Please specify variable \"directory\" in rTorrent.");
                }

                var item = new DownloadClientItem();
                item.DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this);
                item.Title = torrent.Name;
                item.DownloadId = torrent.Hash;
                item.OutputPath = _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(torrent.Path));
                item.TotalSize = torrent.TotalSize;
                item.RemainingSize = torrent.RemainingSize;
                item.Category = torrent.Category;
                item.SeedRatio = torrent.Ratio;

                if (torrent.DownRate > 0)
                {
                    var secondsLeft = torrent.RemainingSize / torrent.DownRate;
                    item.RemainingTime = TimeSpan.FromSeconds(secondsLeft);
                }
                else
                {
                    item.RemainingTime = TimeSpan.Zero;
                }

                if (torrent.IsFinished)
                {
                    item.Status = DownloadItemStatus.Completed;
                }
                else if (torrent.IsActive)
                {
                    item.Status = DownloadItemStatus.Downloading;
                }
                else if (!torrent.IsActive)
                {
                    item.Status = DownloadItemStatus.Paused;
                }

                // Grab cached seedConfig
                var seedConfig = _downloadSeedConfigProvider.GetSeedConfiguration(torrent.Hash);

                // Check if torrent is finished and if it exceeds cached seedConfig
                item.CanMoveFiles = item.CanBeRemoved =
                    torrent.IsFinished && seedConfig != null &&
                    (
                        (torrent.Ratio / 1000.0) >= seedConfig.Ratio ||
                        (DateTimeOffset.Now - DateTimeOffset.FromUnixTimeSeconds(torrent.FinishedTime)) >= seedConfig.SeedTime
                    );

                items.Add(item);
            }

            return items;
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            if (deleteData)
            {
                DeleteItemData(item);
            }

            _proxy.RemoveTorrent(item.DownloadId, Settings);
        }

        public override DownloadClientInfo GetStatus()
        {
            // XXX: This function's correctness has not been considered

            var status = new DownloadClientInfo
            {
                IsLocalhost = Settings.Host == "127.0.0.1" || Settings.Host == "localhost"
            };

            return status;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.HasErrors()) return;
            failures.AddIfNotNull(TestGetTorrents());
            failures.AddIfNotNull(TestDirectory());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxy.GetVersion(Settings);

                if (new Version(version) < new Version("0.9.0"))
                {
                    return new ValidationFailure(string.Empty, "rTorrent version should be at least 0.9.0. Version reported is {0}", version);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test rTorrent");

                return new NzbDroneValidationFailure("Host", "Unable to connect to rTorrent")
                       {
                           DetailedDescription = ex.Message
                       };
            }

            return null;
        }

        private ValidationFailure TestGetTorrents()
        {
            try
            {
                _proxy.GetTorrents(Settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get torrents");
                return new NzbDroneValidationFailure(string.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }

        private ValidationFailure TestDirectory()
        {
            var result = _rTorrentDirectoryValidator.Validate(Settings);

            if (result.IsValid)
            {
                return null;
            }

            return result.Errors.First();
        }

        private bool WaitForTorrent(string hash, int tries, int retryDelay)
        {
            for (var i = 0; i < tries; i++)
            {
                if (_proxy.HasHashTorrent(hash, Settings))
                {
                    return true;
                }

                Thread.Sleep(retryDelay);
            }

            _logger.LogDebug("Could not find hash {Hash} in {Tries} tries at {RetryDelay} ms intervals.", hash, tries, retryDelay);

            return false;
        }
    }
}
