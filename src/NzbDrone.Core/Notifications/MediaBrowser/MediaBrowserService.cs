using System;
using System.Net;
using Microsoft.Extensions.Logging;
using FluentValidation.Results;
using NzbDrone.Common.Http;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.Emby
{
    public interface IMediaBrowserService
    {
        void Notify(MediaBrowserSettings settings, string title, string message);
        void Update(MediaBrowserSettings settings, Series series, string updateType);
        ValidationFailure Test(MediaBrowserSettings settings);
    }

    public class MediaBrowserService : IMediaBrowserService
    {
        private readonly IMediaBrowserProxy _proxy;
        private readonly ILogger<MediaBrowserService> _logger;
        
        public MediaBrowserService(IMediaBrowserProxy proxy, ILogger<MediaBrowserService> logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public void Notify(MediaBrowserSettings settings, string title, string message)
        {
            _proxy.Notify(settings, title, message);
        }

        public void Update(MediaBrowserSettings settings, Series series, string updateType)
        {
            _proxy.Update(settings, series.Path, updateType);
        }

        public ValidationFailure Test(MediaBrowserSettings settings)
        {
            try
            {
                _logger.LogDebug("Testing connection to MediaBrowser: {Address}", settings.Address);

                Notify(settings, "Test from Sonarr", "Success! MediaBrowser has been successfully configured!");
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new ValidationFailure("ApiKey", "API Key is incorrect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send test message");
                return new ValidationFailure("Host", "Unable to send test message: " + ex.Message);
            }

            return null;
        }
    }
}
