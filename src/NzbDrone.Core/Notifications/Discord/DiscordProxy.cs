using Microsoft.Extensions.Logging;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Notifications.Discord.Payloads;

namespace NzbDrone.Core.Notifications.Discord
{
    public interface IDiscordProxy
    {
        void SendPayload(DiscordPayload payload, DiscordSettings settings);
    }

    public class DiscordProxy : IDiscordProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger<DiscordProxy> _logger;

        public DiscordProxy(IHttpClient httpClient, ILogger<DiscordProxy> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public void SendPayload(DiscordPayload payload, DiscordSettings settings)
        {
            try
            {
                var request = new HttpRequestBuilder(settings.WebHookUrl)
                    .Accept(HttpAccept.Json)
                    .Build();

                request.Method = HttpMethod.POST;
                request.Headers.ContentType = "application/json";
                request.SetContent(payload.ToJson());

                 _httpClient.Execute(request);
            }
            catch (HttpException ex)
            {
                _logger.LogError(ex, "Unable to post payload {Payload}", payload.ToJson());
                throw new DiscordException("Unable to post payload", ex);
            }
        }
    }
}
