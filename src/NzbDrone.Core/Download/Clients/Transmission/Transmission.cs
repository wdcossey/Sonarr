using System;
using System.Text.RegularExpressions;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.Transmission
{
    public class Transmission : TransmissionBase
    {
        public Transmission(ITransmissionProxy proxy,
                            ITorrentFileInfoReader torrentFileInfoReader,
                            IHttpClient httpClient,
                            IConfigService configService,
                            IDiskProvider diskProvider,
                            IRemotePathMappingService remotePathMappingService,
                            ILogger<Transmission> logger)
            : base(proxy, torrentFileInfoReader, httpClient, configService, diskProvider, remotePathMappingService, logger)
        {
        }

        protected override ValidationFailure ValidateVersion()
        {
            var versionString = _proxy.GetClientVersion(Settings);

            _logger.LogDebug("Transmission version information: {VersionString}", versionString);

            var versionResult = Regex.Match(versionString, @"(?<!\(|(\d|\.)+)(\d|\.)+(?!\)|(\d|\.)+)").Value;
            var version = Version.Parse(versionResult);

            if (version < new Version(2, 40))
            {
                return new ValidationFailure(string.Empty, "Transmission version not supported, should be 2.40 or higher.");
            }

            return null;
        }

        public override string Name => "Transmission";
    }
}
