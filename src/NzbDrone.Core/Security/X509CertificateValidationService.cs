using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Security
{
    public class X509CertificateValidationService : IHandle<ApplicationStartedEvent>
    {
        private readonly IConfigService _configService;
        private readonly ILogger<X509CertificateValidationService> _logger;

        public X509CertificateValidationService(IConfigService configService, ILogger<X509CertificateValidationService> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        private bool ShouldByPassValidationError(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var request = sender as HttpWebRequest;

            if (request == null)
            {
                return true;
            }

            if (certificate is X509Certificate2 cert2 && cert2.SignatureAlgorithm.FriendlyName == "md5RSA")
            {
                _logger.LogError("https://{Authority} uses the obsolete md5 hash in it's https certificate, if that is your certificate, please (re)create certificate with better algorithm as soon as possible.", request.RequestUri.Authority);
            }

            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (request.RequestUri.Host == "localhost" || request.RequestUri.Host == "127.0.0.1")
            {
                return true;
            }

            var ipAddresses = GetIPAddresses(request.RequestUri.Host);
            var certificateValidation = _configService.CertificateValidation;

            if (certificateValidation == CertificateValidationType.Disabled)
            {
                return true;
            }

            if (certificateValidation == CertificateValidationType.DisabledForLocalAddresses &&
                ipAddresses.All(i => i.IsLocalAddress()))
            {
                return true;
            }

            _logger.LogError("Certificate validation for {Address} failed. {SslPolicyErrors}", request.Address, sslPolicyErrors);

            return false;
        }

        private IPAddress[] GetIPAddresses(string host)
        {
            if (IPAddress.TryParse(host, out var ipAddress))
            {
                return new []{ ipAddress };
            }

            return Dns.GetHostEntry(host).AddressList;
        }

        public void Handle(ApplicationStartedEvent message)
        {
            ServicePointManager.ServerCertificateValidationCallback = ShouldByPassValidationError;
        }
    }
}
