using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients.Transmission;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.Vuze
{
    public class Vuze : TransmissionBase
    {
        private const int MINIMUM_SUPPORTED_PROTOCOL_VERSION = 14;

        public Vuze(ITransmissionProxy proxy,
                    ITorrentFileInfoReader torrentFileInfoReader,
                    IHttpClient<Vuze> httpClient,
                    IConfigService configService,
                    IDiskProvider diskProvider,
                    IRemotePathMappingService remotePathMappingService,
                    ILogger<Vuze> logger)
            : base(proxy, torrentFileInfoReader, httpClient, configService, diskProvider, remotePathMappingService, logger)
        {
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            _proxy.RemoveTorrent(item.DownloadId, deleteData, Settings);
        }

        protected override OsPath GetOutputPath(OsPath outputPath, TransmissionTorrent torrent)
        {
            // Vuze has similar behavior as uTorrent:
            // - A multi-file torrent is downloaded in a job folder and 'outputPath' points to that directory directly.
            // - A single-file torrent is downloaded in the root folder and 'outputPath' poinst to that root folder.
            // We have to make sure the return value points to the job folder OR file.
            if (outputPath == null || outputPath.FileName == torrent.Name || torrent.FileCount > 1)
            {
                _logger.LogTrace("Vuze output directory: {OutputPath}", outputPath);
            }
            else
            {
                outputPath = outputPath + torrent.Name;
                _logger.LogTrace("Vuze output file: {OutputPath}", outputPath);
            }

            return outputPath;
        }

        protected override ValidationFailure ValidateVersion()
        {
            var versionString = _proxy.GetProtocolVersion(Settings);

            _logger.LogDebug("Vuze protocol version information: {VersionString}", versionString);

            int version;
            if (!int.TryParse(versionString, out version) || version < MINIMUM_SUPPORTED_PROTOCOL_VERSION)
            {
                {
                    return new ValidationFailure(string.Empty, "Protocol version not supported, use Vuze 5.0.0.0 or higher with Vuze Web Remote plugin.");
                }
            }

            return null;
        }

        public override string Name => "Vuze";
    }
}
