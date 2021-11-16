using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Notifications.Xbmc.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.Xbmc
{
    public class JsonApiProvider : IApiProvider
    {
        private readonly IXbmcJsonApiProxy _proxy;
        private readonly ILogger<JsonApiProvider> _logger;

        public JsonApiProvider(IXbmcJsonApiProxy proxy, ILogger<JsonApiProvider> logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public bool CanHandle(XbmcVersion version)
        {
            return version >= new XbmcVersion(5);
        }

        public void Notify(XbmcSettings settings, string title, string message)
        {
            _proxy.Notify(settings, title, message);
        }

        public void Update(XbmcSettings settings, Series series)
        {
            if (!settings.AlwaysUpdate)
            {
                _logger.LogDebug("Determining if there are any active players on XBMC host: {Address}", settings.Address);
                var activePlayers = GetActivePlayers(settings);

                if (activePlayers.Any(a => a.Type.Equals("video")))
                {
                    _logger.LogDebug("Video is currently playing, skipping library update");
                    return;
                }
            }

            UpdateLibrary(settings, series);
        }

        public void Clean(XbmcSettings settings)
        {
            if (!settings.AlwaysUpdate)
            {
                _logger.LogDebug("Determining if there are any active players on XBMC host: {Address}", settings.Address);
                var activePlayers = GetActivePlayers(settings);

                if (activePlayers.Any(a => a.Type.Equals("video")))
                {
                    _logger.LogDebug("Video is currently playing, skipping library cleaning");
                    return;
                }
            }

            _proxy.CleanLibrary(settings);
        }

        public List<ActivePlayer> GetActivePlayers(XbmcSettings settings)
        {
            return _proxy.GetActivePlayers(settings);
        }

        public string GetSeriesPath(XbmcSettings settings, Series series)
        {
            var allSeries = _proxy.GetSeries(settings);

            if (!allSeries.Any())
            {
                _logger.LogDebug("No TV shows returned from XBMC");
                return null;
            }

            var matchingSeries = allSeries.FirstOrDefault(s =>
            {
                var tvdbId = 0;
                int.TryParse(s.ImdbNumber, out tvdbId);

                return tvdbId == series.TvdbId || s.Label == series.Title;
            });

            return matchingSeries?.File;
        }

        private void UpdateLibrary(XbmcSettings settings, Series series)
        {
            try
            {
                var seriesPath = GetSeriesPath(settings, series);

                if (seriesPath != null)
                {
                    _logger.LogDebug("Updating series {Series} (Path: {SeriesPath}) on XBMC host: {Address}", series, seriesPath, settings.Address);
                }

                else
                {
                    _logger.LogDebug("Series {Series} doesn't exist on XBMC host: {Address}, Updating Entire Library", series,
                                 settings.Address);
                }

                var response = _proxy.UpdateLibrary(settings, seriesPath);

                if (!response.Equals("OK", StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogDebug("Failed to update library for: {Address}", settings.Address);
                }
            }

            catch (Exception ex)
            {
                _logger.LogDebug(ex, "{Message}", ex.Message);
            }
        }
    }
}
