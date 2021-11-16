using System;
using System.Net;
using Microsoft.Extensions.Logging;
using MonoTorrent;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download
{
    public abstract class TorrentClientBase<TSettings> : DownloadClientBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected readonly IHttpClient _httpClient;
        protected readonly ITorrentFileInfoReader _torrentFileInfoReader;

        protected TorrentClientBase(ITorrentFileInfoReader torrentFileInfoReader,
                                    IHttpClient httpClient,
                                    IConfigService configService,
                                    IDiskProvider diskProvider,
                                    IRemotePathMappingService remotePathMappingService,
                                    ILogger logger)
            : base(configService, diskProvider, remotePathMappingService, logger)
        {
            _httpClient = httpClient;
            _torrentFileInfoReader = torrentFileInfoReader;
        }

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public virtual bool PreferTorrentFile => false;

        protected abstract string AddFromMagnetLink(RemoteEpisode remoteEpisode, string hash, string magnetLink);
        protected abstract string AddFromTorrentFile(RemoteEpisode remoteEpisode, string hash, string filename, byte[] fileContent);

        public override string Download(RemoteEpisode remoteEpisode)
        {
            var torrentInfo = remoteEpisode.Release as TorrentInfo;

            string magnetUrl = null;
            string torrentUrl = null;

            if (remoteEpisode.Release.DownloadUrl.IsNotNullOrWhiteSpace() && remoteEpisode.Release.DownloadUrl.StartsWith("magnet:"))
            {
                magnetUrl = remoteEpisode.Release.DownloadUrl;
            }
            else
            {
                torrentUrl = remoteEpisode.Release.DownloadUrl;
            }

            if (torrentInfo != null && !torrentInfo.MagnetUrl.IsNullOrWhiteSpace())
            {
                magnetUrl = torrentInfo.MagnetUrl;
            }

            if (PreferTorrentFile)
            {
                if (torrentUrl.IsNotNullOrWhiteSpace())
                {
                    try
                    {
                        return DownloadFromWebUrl(remoteEpisode, torrentUrl);
                    }
                    catch (Exception ex)
                    {
                        if (!magnetUrl.IsNullOrWhiteSpace())
                        {
                            throw;
                        }

                        _logger.LogDebug("Torrent download failed, trying magnet. ({Message})", ex.Message);
                    }
                }

                if (magnetUrl.IsNotNullOrWhiteSpace())
                {
                    try
                    {
                        return DownloadFromMagnetUrl(remoteEpisode, magnetUrl);
                    }
                    catch (NotSupportedException ex)
                    {
                        throw new ReleaseDownloadException(remoteEpisode.Release, "Magnet not supported by download client. ({0})", ex.Message);
                    }
                }
            }
            else
            {
                if (magnetUrl.IsNotNullOrWhiteSpace())
                {
                    try
                    {
                        return DownloadFromMagnetUrl(remoteEpisode, magnetUrl);
                    }
                    catch (NotSupportedException ex)
                    {
                        if (torrentUrl.IsNullOrWhiteSpace())
                        {
                            throw new ReleaseDownloadException(remoteEpisode.Release, "Magnet not supported by download client. ({0})", ex.Message);
                        }

                        _logger.LogDebug("Magnet not supported by download client, trying torrent. ({Message})", ex.Message);
                    }
                }

                if (torrentUrl.IsNotNullOrWhiteSpace())
                {
                    return DownloadFromWebUrl(remoteEpisode, torrentUrl);
                }
            }

            return null;
        }

        private string DownloadFromWebUrl(RemoteEpisode remoteEpisode, string torrentUrl)
        {
            byte[] torrentFile = null;

            try
            {
                var request = new HttpRequest(torrentUrl);
                request.RateLimitKey = remoteEpisode?.Release?.IndexerId.ToString();
                request.Headers.Accept = "application/x-bittorrent";
                request.AllowAutoRedirect = false;

                var response = _httpClient.Get(request);

                if (response.StatusCode == HttpStatusCode.MovedPermanently ||
                    response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.SeeOther)
                {
                    var locationHeader = response.Headers.GetSingleValue("Location");

                    _logger.LogTrace("Torrent request is being redirected to: {LocationHeader}", locationHeader);

                    if (locationHeader != null)
                    {
                        if (locationHeader.StartsWith("magnet:"))
                        {
                            return DownloadFromMagnetUrl(remoteEpisode, locationHeader);
                        }

                        return DownloadFromWebUrl(remoteEpisode, locationHeader);
                    }

                    throw new WebException("Remote website tried to redirect without providing a location.");
                }

                torrentFile = response.ResponseData;

                _logger.LogDebug("Downloading torrent for episode '{Title}' finished ({Length} bytes from {TorrentUrl})", remoteEpisode.Release.Title, torrentFile.Length, torrentUrl);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogError(ex, "Downloading torrent file for episode '{Title}' failed since it no longer exists ({TorrentUrl})", remoteEpisode.Release.Title, torrentUrl);
                    throw new ReleaseUnavailableException(remoteEpisode.Release, "Downloading torrent failed", ex);
                }

                if ((int)ex.Response.StatusCode == 429)
                {
                    _logger.LogError("API Grab Limit reached for {TorrentUrl}", torrentUrl);
                }
                else
                {
                    _logger.LogError(ex, "Downloading torrent file for episode '{Title}' failed ({TorrentUrl})", remoteEpisode.Release.Title, torrentUrl);
                }

                throw new ReleaseDownloadException(remoteEpisode.Release, "Downloading torrent failed", ex);
            }
            catch (WebException ex)
            {
                _logger.LogError(ex, "Downloading torrent file for episode '{Title}' failed ({TorrentUrl})", remoteEpisode.Release.Title, torrentUrl);

                throw new ReleaseDownloadException(remoteEpisode.Release, "Downloading torrent failed", ex);
            }

            var filename = string.Format("{0}.torrent", FileNameBuilder.CleanFileName(remoteEpisode.Release.Title));
            var hash = _torrentFileInfoReader.GetHashFromTorrentFile(torrentFile);
            var actualHash = AddFromTorrentFile(remoteEpisode, hash, filename, torrentFile);

            if (actualHash.IsNotNullOrWhiteSpace() && hash != actualHash)
            {
                _logger.LogDebug(
                    "{Implementation} did not return the expected InfoHash for '{DownloadUrl}', Sonarr could potentially lose track of the download in progress.",
                    Definition.Implementation, remoteEpisode.Release.DownloadUrl);
            }

            return actualHash;
        }

        private string DownloadFromMagnetUrl(RemoteEpisode remoteEpisode, string magnetUrl)
        {
            string hash = null;
            string actualHash = null;

            try
            {
                hash = new MagnetLink(magnetUrl).InfoHash.ToHex();
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Failed to parse magnetlink for episode '{Title}': '{MagnetUrl}'", remoteEpisode.Release.Title, magnetUrl);

                return null;
            }

            if (hash != null)
            {
                actualHash = AddFromMagnetLink(remoteEpisode, hash, magnetUrl);
            }

            if (actualHash.IsNotNullOrWhiteSpace() && hash != actualHash)
            {
                _logger.LogDebug(
                    "{Implementation} did not return the expected InfoHash for '{DownloadUrl}', Sonarr could potentially lose track of the download in progress.",
                    Definition.Implementation, remoteEpisode.Release.DownloadUrl);
            }

            return actualHash;
        }
    }
}
