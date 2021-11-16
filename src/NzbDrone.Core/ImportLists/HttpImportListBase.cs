using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Http.CloudFlare;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists
{
    public abstract class HttpImportListBase<TSettings> : ImportListBase<TSettings>
        where TSettings : IImportListSettings, new()
    {
        protected const int MaxNumResultsPerQuery = 1000;

        protected readonly IHttpClient _httpClient;

        public bool SupportsPaging => PageSize > 0;

        public virtual int PageSize => 0;
        public virtual TimeSpan RateLimit => TimeSpan.FromSeconds(2);

        public abstract IImportListRequestGenerator GetRequestGenerator();
        public abstract IParseImportListResponse GetParser();

        public HttpImportListBase(IHttpClient httpClient, IImportListStatusService importListStatusService, IConfigService configService, IParsingService parsingService, ILogger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _httpClient = httpClient;
        }

        public override IList<ImportListItemInfo> Fetch()
        {
            return FetchItems(g => g.GetListItems(), true);
        }

        protected virtual IList<ImportListItemInfo> FetchItems(Func<IImportListRequestGenerator, ImportListPageableRequestChain> pageableRequestChainSelector, bool isRecent = false)
        {
            var releases = new List<ImportListItemInfo>();
            var url = string.Empty;

            try
            {
                var generator = GetRequestGenerator();
                var parser = GetParser();

                var pageableRequestChain = pageableRequestChainSelector(generator);

                for (int i = 0; i < pageableRequestChain.Tiers; i++)
                {
                    var pageableRequests = pageableRequestChain.GetTier(i);

                    foreach (var pageableRequest in pageableRequests)
                    {
                        var pagedReleases = new List<ImportListItemInfo>();

                        foreach (var request in pageableRequest)
                        {
                            url = request.Url.FullUri;

                            var page = FetchPage(request, parser);

                            pagedReleases.AddRange(page);

                            if (pagedReleases.Count >= MaxNumResultsPerQuery)
                            {
                                break;
                            }

                            if (!IsFullPage(page))
                            {
                                break;
                            }
                        }

                        releases.AddRange(pagedReleases.Where(IsValidItem));
                    }

                    if (releases.Any())
                    {
                        break;
                    }
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.NameResolutionFailure ||
                    webException.Status == WebExceptionStatus.ConnectFailure)
                {
                    _importListStatusService.RecordConnectionFailure(Definition.Id);
                }
                else
                {
                    _importListStatusService.RecordFailure(Definition.Id);
                }

                if (webException.Message.Contains("502") || webException.Message.Contains("503") ||
                    webException.Message.Contains("timed out"))
                {
                    _logger.LogWarning("{Type} server is currently unavailable. {Url} {Message}", this.GetType(), url, webException.Message);
                }
                else
                {
                    _logger.LogWarning("{Type} {Url} {Message}", this.GetType(), url, webException.Message);
                }
            }
            catch (TooManyRequestsException ex)
            {
                if (ex.RetryAfter != TimeSpan.Zero)
                {
                    _importListStatusService.RecordFailure(Definition.Id, ex.RetryAfter);
                }
                else
                {
                    _importListStatusService.RecordFailure(Definition.Id, TimeSpan.FromHours(1));
                }
                _logger.LogWarning("API Request Limit reached for {Type}", this.GetType());
            }
            catch (HttpException ex)
            {
                _importListStatusService.RecordFailure(Definition.Id);
                _logger.LogWarning("{Type} {Message}", this.GetType(), ex.Message);
            }
            catch (RequestLimitReachedException)
            {
                _importListStatusService.RecordFailure(Definition.Id, TimeSpan.FromHours(1));
                _logger.LogWarning("API Request Limit reached for {Type}", this.GetType());
            }
            catch (CloudFlareCaptchaException ex)
            {
                _importListStatusService.RecordFailure(Definition.Id);
                ex.WithData("FeedUrl", url);
                if (ex.IsExpired)
                {
                    _logger.LogError(ex, "Expired CAPTCHA token for {Type}, please refresh in import list settings.", this.GetType());
                }
                else
                {
                    _logger.LogError(ex, "CAPTCHA token required for {Type}, check import list settings.", this.GetType());
                }
            }
            catch (ImportListException ex)
            {
                _importListStatusService.RecordFailure(Definition.Id);
                _logger.LogWarning(ex, "{Url}", url);
            }
            catch (Exception ex)
            {
                _importListStatusService.RecordFailure(Definition.Id);
                ex.WithData("FeedUrl", url);
                _logger.LogError(ex, "An error occurred while processing feed. {Url}", url);
            }

            return CleanupListItems(releases);
        }

        protected virtual bool IsValidItem(ImportListItemInfo release)
        {
            if (release.Title.IsNullOrWhiteSpace())
            {
                return false;
            }

            return true;
        }

        protected virtual bool IsFullPage(IList<ImportListItemInfo> page)
        {
            return PageSize != 0 && page.Count >= PageSize;
        }

        protected virtual IList<ImportListItemInfo> FetchPage(ImportListRequest request, IParseImportListResponse parser)
        {
            var response = FetchImportListResponse(request);

            return parser.ParseResponse(response).ToList();
        }

        protected virtual ImportListResponse FetchImportListResponse(ImportListRequest request)
        {
            _logger.LogDebug("Downloading Feed {HttpRequestString}", request.HttpRequest.ToString(false));

            if (request.HttpRequest.RateLimit < RateLimit)
            {
                request.HttpRequest.RateLimit = RateLimit;
            }

            return new ImportListResponse(request, _httpClient.Execute(request.HttpRequest));
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
        }

        protected virtual ValidationFailure TestConnection()
        {
            try
            {
                var parser = GetParser();
                var generator = GetRequestGenerator();
                var releases = FetchPage(generator.GetListItems().GetAllTiers().First().First(), parser);

                if (releases.Empty())
                {
                    return new NzbDroneValidationFailure(string.Empty,
                               "No results were returned from your import list, please check your settings.")
                           {IsWarning = true};
                }
            }
            catch (RequestLimitReachedException)
            {
                _logger.LogWarning("Request limit reached");
            }
            catch (UnsupportedFeedException ex)
            {
                _logger.LogWarning(ex, "Import list feed is not supported");

                return new ValidationFailure(string.Empty, "Import list feed is not supported: " + ex.Message);
            }
            catch (ImportListException ex)
            {
                _logger.LogWarning(ex, "Unable to connect to import list");

                return new ValidationFailure(string.Empty, "Unable to connect to import list. " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to connect to import list");

                return new ValidationFailure(string.Empty, "Unable to connect to import list, check the log for more details");
            }

            return null;
        }
    }

}
