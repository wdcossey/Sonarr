using System;
using System.Net;
using System.Web;
using Microsoft.Extensions.Logging;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Notifications.Telegram
{
    public interface ITelegramProxy
    {
        void SendNotification(string title, string message, TelegramSettings settings);
        ValidationFailure Test(TelegramSettings settings);
    }

    public class TelegramProxy : ITelegramProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger<TelegramProxy> _logger;
        private const string TelegramUrl = "https://api.telegram.org";

        public TelegramProxy(IHttpClient httpClient, ILogger<TelegramProxy> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public void SendNotification(string title, string message, TelegramSettings settings)
        {
            //Format text to add the title before and bold using markdown
            var text = $"<b>{HttpUtility.HtmlEncode(title)}</b>\n{HttpUtility.HtmlEncode(message)}";

            var requestBuilder = new HttpRequestBuilder(TelegramUrl).Resource("bot{token}/sendmessage").Post();

            var request = requestBuilder.SetSegment("token", settings.BotToken)
                                        .AddFormParameter("chat_id", settings.ChatId)
                                        .AddFormParameter("parse_mode", "HTML")
                                        .AddFormParameter("text", text)
                                        .AddFormParameter("disable_notification", settings.SendSilently)
                                        .Build();

            _httpClient.Post(request);
        }

        public ValidationFailure Test(TelegramSettings settings)
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

                if (ex is WebException webException)
                {
                    return new ValidationFailure("Connection", $"{webException.Status.ToString()}: {webException.Message}");
                }
                else if (ex is HttpException restException && restException.Response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var error = Json.Deserialize<TelegramError>(restException.Response.Content);
                    var property = error.Description.ContainsIgnoreCase("chat not found") ? "ChatId" : "BotToken";

                    return new ValidationFailure(property, error.Description);
                }

                return new ValidationFailure("BotToken", "Unable to send test message");
            }

            return null;
        }
    }
}
