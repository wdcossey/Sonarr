using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using FluentValidation.Results;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.Email
{
    public class Email : NotificationBase<EmailSettings>
    {
        private readonly ILogger<Email> _logger;

        public override string Name => "Email";


        public Email(ILogger<Email> logger)
        {
            _logger = logger;
        }

        public override string Link => null;

        public override void OnGrab(GrabMessage grabMessage)
        {
            var body = $"{grabMessage.Message} sent to queue.";

            SendEmail(Settings, EPISODE_GRABBED_TITLE_BRANDED, body);
        }

        public override void OnDownload(DownloadMessage message)
        {
            var body = $"{message.Message} Downloaded and sorted.";

            SendEmail(Settings, EPISODE_DOWNLOADED_TITLE_BRANDED, body);
        }

        public override void OnEpisodeFileDelete(EpisodeDeleteMessage deleteMessage)
        {
            var body = $"{deleteMessage.Message} deleted.";

            SendEmail(Settings, EPISODE_DELETED_TITLE_BRANDED, body);
        }

        public override void OnSeriesDelete(SeriesDeleteMessage deleteMessage)
        {
            var body = $"{deleteMessage.Message}";

            SendEmail(Settings, SERIES_DELETED_TITLE_BRANDED, body);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck message)
        {
            SendEmail(Settings, HEALTH_ISSUE_TITLE_BRANDED, message.Message);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(Test(Settings));

            return new ValidationResult(failures);
        }

        private void SendEmail(EmailSettings settings, string subject, string body, bool htmlBody = false)
        {
            var email = new MimeMessage();

            email.From.Add(ParseAddress("From", settings.From));
            email.To.AddRange(settings.To.Select(x => ParseAddress("To", x)));
            email.Cc.AddRange(settings.Cc.Select(x => ParseAddress("CC", x)));
            email.Bcc.AddRange(settings.Bcc.Select(x => ParseAddress("BCC", x)));

            email.Subject = subject;
            email.Body = new TextPart(htmlBody ? "html" : "plain")
            {
                Text = body
            };

            _logger.LogDebug("Sending email Subject: {Subject}", subject);

            try
            {
                Send(email, settings);
                _logger.LogDebug("Email sent. Subject: {Subject}", subject);

            }
            catch (Exception ex)
            {
                _logger.LogError("Error sending email. Subject: {Subject}", email.Subject);
                _logger.LogDebug(ex, "{Message}", ex.Message);
                throw;
            }

            _logger.LogDebug("Finished sending email. Subject: {Subject}", subject);
        }

        private void Send(MimeMessage email, EmailSettings settings)
        {
            using (var client = new SmtpClient())
            {
                client.Timeout = 10000;

                var serverOption = SecureSocketOptions.Auto;

                if (settings.RequireEncryption)
                {
                    serverOption = settings.Port == 465
                        ? SecureSocketOptions.SslOnConnect
                        : SecureSocketOptions.StartTls;
                }

                _logger.LogDebug("Connecting to mail server");

                client.Connect(settings.Server, settings.Port, serverOption);

                if (!string.IsNullOrWhiteSpace(settings.Username))
                {
                    _logger.LogDebug("Authenticating to mail server");

                    client.Authenticate(settings.Username, settings.Password);
                }

                _logger.LogDebug("Sending to mail server");


                client.Send(email);

                _logger.LogDebug("Sent to mail server, disconnecting");

                client.Disconnect(true);

                _logger.LogDebug("Disconnecting from mail server");
            }
        }

        public ValidationFailure Test(EmailSettings settings)
        {
            const string body = "Success! You have properly configured your email notification settings";

            try
            {
                SendEmail(settings, "Sonarr - Test Notification", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send test email");
                return new ValidationFailure("Server", "Unable to send test email");
            }

            return null;
        }

        private MailboxAddress ParseAddress(string type, string address)
        {
            try
            {
                return MailboxAddress.Parse(address);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Type} email address '{Address}' invalid", type, address);
                throw;
            }
        }
    }
}
