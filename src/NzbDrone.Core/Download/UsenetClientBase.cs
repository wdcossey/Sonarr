using System.Net;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download
{
    public abstract class UsenetClientBase<TSettings> : DownloadClientBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected readonly IHttpClient _httpClient;
        private readonly IValidateNzbs _nzbValidationService;

        protected UsenetClientBase(IHttpClient httpClient,
                                   IConfigService configService,
                                   IDiskProvider diskProvider,
                                   IRemotePathMappingService remotePathMappingService,
                                   IValidateNzbs nzbValidationService,
                                   ILogger logger)
            : base(configService, diskProvider, remotePathMappingService, logger)
        {
            _httpClient = httpClient;
            _nzbValidationService = nzbValidationService;
        }

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

        protected abstract string AddFromNzbFile(RemoteEpisode remoteEpisode, string filename, byte[] fileContent);

        public override string Download(RemoteEpisode remoteEpisode)
        {
            var url = remoteEpisode.Release.DownloadUrl;
            var filename =  FileNameBuilder.CleanFileName(remoteEpisode.Release.Title) + ".nzb";

            byte[] nzbData;

            try
            {
                var request = new HttpRequest(url);
                request.RateLimitKey = remoteEpisode?.Release?.IndexerId.ToString();
                nzbData = _httpClient.Get(request).ResponseData;

                _logger.LogDebug("Downloaded nzb for episode '{Title}' finished ({Length} bytes from {Url})", remoteEpisode.Release.Title, nzbData.Length, url);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogError(ex, "Downloading nzb file for episode '{Title}' failed since it no longer exists ({Url})", remoteEpisode.Release.Title, url);
                    throw new ReleaseUnavailableException(remoteEpisode.Release, "Downloading nzb failed", ex);
                }

                if ((int)ex.Response.StatusCode == 429)
                {
                    _logger.LogError("API Grab Limit reached for {Url}", url);
                }
                else
                {
                    _logger.LogError(ex, "Downloading nzb for episode '{Title}' failed ({Url})", remoteEpisode.Release.Title, url);
                }

                throw new ReleaseDownloadException(remoteEpisode.Release, "Downloading nzb failed", ex);
            }
            catch (WebException ex)
            {
                _logger.LogError(ex, "Downloading nzb for episode '{Title}' failed ({Url})", remoteEpisode.Release.Title, url);

                throw new ReleaseDownloadException(remoteEpisode.Release, "Downloading nzb failed", ex);
            }

            _nzbValidationService.Validate(filename, nzbData);

            _logger.LogInformation("Adding report [{Title}] to the queue.", remoteEpisode.Release.Title);
            return AddFromNzbFile(remoteEpisode, filename, nzbData);
        }
    }
}
