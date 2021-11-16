using System;
using Microsoft.Extensions.Logging;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Notifications.Pushover
{
    public interface IPushoverProxy
    {
        void SendNotification(string title, string message, PushoverSettings settings);
        ValidationFailure Test(PushoverSettings settings);
    }

    public class PushoverProxy : IPushoverProxy
    {
        private readonly IHttpClient<PushoverProxy> _httpClient;
        private readonly ILogger<PushoverProxy> _logger;
        private const string PushoverUrl = "https://api.pushover.net/1/messages.json";
        
        public PushoverProxy(IHttpClient<PushoverProxy> httpClient, ILogger<PushoverProxy> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public void SendNotification(string title, string message, PushoverSettings settings)
        {
            var requestBuilder = new HttpRequestBuilder(PushoverUrl).Post();

            requestBuilder.AddFormParameter("token", settings.ApiKey)
                          .AddFormParameter("user", settings.UserKey)
                          .AddFormParameter("device", string.Join(",", settings.Devices))
                          .AddFormParameter("title", title)
                          .AddFormParameter("message", message)
                          .AddFormParameter("priority", settings.Priority);

            if ((PushoverPriority)settings.Priority == PushoverPriority.Emergency)
            {
                requestBuilder.AddFormParameter("retry", settings.Retry);
                requestBuilder.AddFormParameter("expire", settings.Expire);
            }

            if (!settings.Sound.IsNullOrWhiteSpace())
            {
                requestBuilder.AddFormParameter("sound", settings.Sound);
            }


            var request = requestBuilder.Build();

            _httpClient.Post(request);
        }

        public ValidationFailure Test(PushoverSettings settings)
        {
            try
            {
                const string title = "Test Notification";
                const string body = "This is a test message from Sonarr";

                SendNotification(title, body, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send test message");
                return new ValidationFailure("ApiKey", "Unable to send test message");
            }

            return null;
        }
    }
}
