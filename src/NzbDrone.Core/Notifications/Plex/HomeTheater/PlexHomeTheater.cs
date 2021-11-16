using System.Collections.Generic;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Notifications.Xbmc;

namespace NzbDrone.Core.Notifications.Plex.HomeTheater
{
    public class PlexHomeTheater : NotificationBase<PlexHomeTheaterSettings>
    {
        private readonly IXbmcService _xbmcService;
        private readonly ILogger<PlexHomeTheater> _logger;

        public PlexHomeTheater(IXbmcService xbmcService, ILogger<PlexHomeTheater> logger)
        {
            _xbmcService = xbmcService;
            _logger = logger;
        }

        public override string Name => "Plex Home Theater";
        public override string Link => "https://plex.tv/";

        public override void OnGrab(GrabMessage grabMessage)
        {
            Notify(Settings, EPISODE_GRABBED_TITLE_BRANDED, grabMessage.Message);
        }

        public override void OnDownload(DownloadMessage message)
        {
            Notify(Settings, EPISODE_DOWNLOADED_TITLE_BRANDED, message.Message);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_xbmcService.Test(Settings, "Success! PHT has been successfully configured!"));

            return new ValidationResult(failures);
        }

        private void Notify(XbmcSettings settings, string header, string message)
        {
            try
            {
                if (Settings.Notify)
                {
                    _xbmcService.Notify(Settings, header, message);
                }
            }
            catch (SocketException ex)
            {
                _logger.LogDebug(ex, "Unable to connect to PHT Host: {Host}:{Port}", Settings.Host, Settings.Port);
            }
        }
    }
}
