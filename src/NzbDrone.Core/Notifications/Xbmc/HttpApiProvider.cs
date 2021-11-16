using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Http;
using NzbDrone.Core.Notifications.Xbmc.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.Xbmc
{
    public class HttpApiProvider : IApiProvider
    {
        private readonly IHttpProvider _httpProvider;
        private readonly ILogger<HttpApiProvider> _logger;

        public HttpApiProvider(IHttpProvider httpProvider, ILogger<HttpApiProvider> logger)
        {
            _httpProvider = httpProvider;
            _logger = logger;
        }

        public bool CanHandle(XbmcVersion version)
        {
            return version < new XbmcVersion(5);
        }

        public void Notify(XbmcSettings settings, string title, string message)
        {
            var notification =
                $"Notification({title},{message},{settings.DisplayTime * 1000},https://raw.github.com/Sonarr/Sonarr/develop/Logo/64.png)";
            var command = BuildExecBuiltInCommand(notification);

            SendCommand(settings, command);
        }

        public void Update(XbmcSettings settings, Series series)
        {
            if (!settings.AlwaysUpdate)
            {
                _logger.LogDebug("Determining if there are any active players on XBMC host: {0}", settings.Address);
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

            const string cleanVideoLibrary = "CleanLibrary(video)";
            var command = BuildExecBuiltInCommand(cleanVideoLibrary);

            SendCommand(settings, command);
        }

        internal List<ActivePlayer> GetActivePlayers(XbmcSettings settings)
        {
            try
            {
                var result = new List<ActivePlayer>();
                var response = SendCommand(settings, "getcurrentlyplaying");

                if (response.Contains("<li>Filename:[Nothing Playing]")) return new List<ActivePlayer>();
                if (response.Contains("<li>Type:Video")) result.Add(new ActivePlayer(1, "video"));

                return result;
            }

            catch (Exception ex)
            {
                _logger.LogDebug(ex, "{Message}", ex.Message);
            }

            return new List<ActivePlayer>();
        }

        internal string GetSeriesPath(XbmcSettings settings, Series series)
        {
            var query =
                $"select path.strPath from path, tvshow, tvshowlinkpath where tvshow.c12 = {series.TvdbId} and tvshowlinkpath.idShow = tvshow.idShow and tvshowlinkpath.idPath = path.idPath";
            var command = $"QueryVideoDatabase({query})";

            const string setResponseCommand =
                "SetResponseFormat(webheader;false;webfooter;false;header;<xml>;footer;</xml>;opentag;<tag>;closetag;</tag>;closefinaltag;false)";
            const string resetResponseCommand = "SetResponseFormat()";

            SendCommand(settings, setResponseCommand);
            var response = SendCommand(settings, command);
            SendCommand(settings, resetResponseCommand);

            if (string.IsNullOrEmpty(response))
                return string.Empty;

            var xDoc = XDocument.Load(new StringReader(response.Replace("&", "&amp;")));
            var xml = xDoc.Descendants("xml").Select(x => x).FirstOrDefault();

            if (xml == null)
                return null;

            var field = xml.Descendants("field").FirstOrDefault();

            if (field == null)
                return null;

            return field.Value;
        }

        internal bool CheckForError(string response)
        {
            _logger.LogDebug("Looking for error in response: {Response}", response);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogDebug("Invalid response from XBMC, the response is not valid JSON");
                return true;
            }

            var errorIndex = response.IndexOf("Error", StringComparison.InvariantCultureIgnoreCase);

            if (errorIndex > -1)
            {
                var errorMessage = response.Substring(errorIndex + 6);
                errorMessage = errorMessage.Substring(0, errorMessage.IndexOfAny(new char[] { '<', ';' }));

                _logger.LogDebug("Error found in response: {ErrorMessage}", errorMessage);
                return true;
            }

            return false;
        }

        private void UpdateLibrary(XbmcSettings settings, Series series)
        {
            try
            {
                _logger.LogDebug("Sending Update DB Request to XBMC Host: {Address}", settings.Address);
                var xbmcSeriesPath = GetSeriesPath(settings, series);

                //If the path is found update it, else update the whole library
                if (!string.IsNullOrEmpty(xbmcSeriesPath))
                {
                    _logger.LogDebug("Updating series [{Series}] on XBMC host: {Address}", series, settings.Address);
                    var command = BuildExecBuiltInCommand(string.Format("UpdateLibrary(video,{0})", xbmcSeriesPath));
                    SendCommand(settings, command);
                }

                else
                {
                    //Update the entire library
                    _logger.LogDebug("Series [{Series}] doesn't exist on XBMC host: {Address}, Updating Entire Library", series, settings.Address);
                    var command = BuildExecBuiltInCommand("UpdateLibrary(video)");
                    SendCommand(settings, command);
                }
            }

            catch (Exception ex)
            {
                _logger.LogDebug(ex, "{Message}",  ex.Message);
            }
        }

        private string SendCommand(XbmcSettings settings, string command)
        {
            var url = HttpRequestBuilder.BuildBaseUrl(settings.UseSsl, settings.Host, settings.Port, $"xbmcCmds/xbmcHttp?command={command}");

            if (!string.IsNullOrEmpty(settings.Username))
            {
                return _httpProvider.DownloadString(url, settings.Username, settings.Password);
            }

            return _httpProvider.DownloadString(url);
        }

        private string BuildExecBuiltInCommand(string command)
        {
            return string.Format("ExecBuiltIn({0})", command);
        }
    }
}
