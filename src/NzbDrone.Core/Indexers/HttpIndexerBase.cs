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
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers
{
    public abstract class HttpIndexerBase<TSettings> : IndexerBase<TSettings>
        where TSettings : IIndexerSettings, new()
    {
        protected const int MaxNumResultsPerQuery = 1000;

        protected readonly IHttpClient _httpClient;

        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public bool SupportsPaging => PageSize > 0;

        public virtual int PageSize => 0;
        public virtual TimeSpan RateLimit => TimeSpan.FromSeconds(2);

        public abstract IIndexerRequestGenerator GetRequestGenerator();
        public abstract IParseIndexerResponse GetParser();

        public HttpIndexerBase(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, ILoggerFactory loggerFactory)
            : base(indexerStatusService, configService, parsingService, loggerFactory)
        {
            _httpClient = httpClient;
        }

        public override IList<ReleaseInfo> FetchRecent()
        {
            if (!SupportsRss)
            {
                return new List<ReleaseInfo>();
            }

            return FetchReleases(g => g.GetRecentRequests(), true);
        }

        public override IList<ReleaseInfo> Fetch(SingleEpisodeSearchCriteria searchCriteria)
        {
            if (!SupportsSearch)
            {
                return new List<ReleaseInfo>();
            }

            return FetchReleases(g => g.GetSearchRequests(searchCriteria));
        }

        public override IList<ReleaseInfo> Fetch(SeasonSearchCriteria searchCriteria)
        {
            if (!SupportsSearch)
            {
                return new List<ReleaseInfo>();
            }

            return FetchReleases(g => g.GetSearchRequests(searchCriteria));
        }

        public override IList<ReleaseInfo> Fetch(DailyEpisodeSearchCriteria searchCriteria)
        {
            if (!SupportsSearch)
            {
                return new List<ReleaseInfo>();
            }

            return FetchReleases(g => g.GetSearchRequests(searchCriteria));
        }

        public override IList<ReleaseInfo> Fetch(DailySeasonSearchCriteria searchCriteria)
        {
            if (!SupportsSearch)
            {
                return new List<ReleaseInfo>();
            }

            return FetchReleases(g => g.GetSearchRequests(searchCriteria));
        }

        public override IList<ReleaseInfo> Fetch(AnimeEpisodeSearchCriteria searchCriteria)
        {
            if (!SupportsSearch)
            {
                return new List<ReleaseInfo>();
            }

            return FetchReleases(g => g.GetSearchRequests(searchCriteria));
        }

        public override IList<ReleaseInfo> Fetch(SpecialEpisodeSearchCriteria searchCriteria)
        {
            if (!SupportsSearch)
            {
                return new List<ReleaseInfo>();
            }

            return FetchReleases(g => g.GetSearchRequests(searchCriteria));
        }

        protected virtual IList<ReleaseInfo> FetchReleases(Func<IIndexerRequestGenerator, IndexerPageableRequestChain> pageableRequestChainSelector, bool isRecent = false)
        {
            var releases = new List<ReleaseInfo>();
            var url = string.Empty;

            try
            {
                var generator = GetRequestGenerator();
                var parser = GetParser();

                var pageableRequestChain = pageableRequestChainSelector(generator);

                var fullyUpdated = false;
                ReleaseInfo lastReleaseInfo = null;
                if (isRecent)
                {
                    lastReleaseInfo = _indexerStatusService.GetLastRssSyncReleaseInfo(Definition.Id);
                }

                for (int i = 0; i < pageableRequestChain.Tiers; i++)
                {
                    var pageableRequests = pageableRequestChain.GetTier(i);

                    foreach (var pageableRequest in pageableRequests)
                    {
                        var pagedReleases = new List<ReleaseInfo>();

                        foreach (var request in pageableRequest)
                        {
                            url = request.Url.FullUri;

                            var page = FetchPage(request, parser);

                            pagedReleases.AddRange(page);

                            if (isRecent && page.Any())
                            {
                                if (lastReleaseInfo == null)
                                {
                                    fullyUpdated = true;
                                    break;
                                }
                                var oldestReleaseDate = page.Select(v => v.PublishDate).Min();
                                if (oldestReleaseDate < lastReleaseInfo.PublishDate || page.Any(v => v.DownloadUrl == lastReleaseInfo.DownloadUrl))
                                {
                                    fullyUpdated = true;
                                    break;
                                }

                                if (pagedReleases.Count >= MaxNumResultsPerQuery &&
                                    oldestReleaseDate < DateTime.UtcNow - TimeSpan.FromHours(24))
                                {
                                    fullyUpdated = false;
                                    break;
                                }
                            }
                            else if (pagedReleases.Count >= MaxNumResultsPerQuery)
                            {
                                break;
                            }

                            if (!IsFullPage(page))
                            {
                                break;
                            }
                        }

                        releases.AddRange(pagedReleases.Where(IsValidRelease));
                    }

                    if (releases.Any())
                    {
                        break;
                    }
                }

                if (isRecent && !releases.Empty())
                {
                    var ordered = releases.OrderByDescending(v => v.PublishDate).ToList();

                    if (!fullyUpdated && lastReleaseInfo != null)
                    {
                        var gapStart = lastReleaseInfo.PublishDate;
                        var gapEnd = ordered.Last().PublishDate;
                        _logger.LogWarning("Indexer {Name} rss sync didn't cover the period between {GapStart} and {GapEnd} UTC. Search may be required.", Definition.Name, gapStart, gapEnd);
                    }
                    lastReleaseInfo = ordered.First();
                    _indexerStatusService.UpdateRssSyncStatus(Definition.Id, lastReleaseInfo);
                }

                _indexerStatusService.RecordSuccess(Definition.Id);
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.NameResolutionFailure ||
                    webException.Status == WebExceptionStatus.ConnectFailure)
                {
                    _indexerStatusService.RecordConnectionFailure(Definition.Id);
                }
                else
                {
                    _indexerStatusService.RecordFailure(Definition.Id);
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
                    _indexerStatusService.RecordFailure(Definition.Id, ex.RetryAfter);
                }
                else
                {
                    _indexerStatusService.RecordFailure(Definition.Id, TimeSpan.FromHours(1));
                }
                _logger.LogWarning("API Request Limit reached for {Type}", this.GetType());
            }
            catch (HttpException ex)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.LogWarning("{Type} {Message}", this.GetType(), ex.Message);
            }
            catch (RequestLimitReachedException)
            {
                _indexerStatusService.RecordFailure(Definition.Id, TimeSpan.FromHours(1));
                _logger.LogWarning("API Request Limit reached for {Type}", this.GetType());
            }
            catch (ApiKeyException)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.LogWarning("Invalid API Key for {Type} {Url}", this.GetType(), url);
            }
            catch (CloudFlareCaptchaException ex)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                ex.WithData("FeedUrl", url);
                if (ex.IsExpired)
                {
                    _logger.LogError(ex, "Expired CAPTCHA token for {Type}, please refresh in indexer settings.", this.GetType());
                }
                else
                {
                    _logger.LogError(ex, "CAPTCHA token required for {Type}, check indexer settings.", this.GetType());
                }
            }
            catch (IndexerException ex)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.LogWarning(ex, "{Url}", url);
            }
            catch (Exception ex)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                ex.WithData("FeedUrl", url);
                _logger.LogError(ex, "An error occurred while processing feed. {Url}", url);
            }

            return CleanupReleases(releases);
        }

        protected virtual bool IsValidRelease(ReleaseInfo release)
        {
            if (release.DownloadUrl.IsNullOrWhiteSpace())
            {
                _logger.LogTrace("Invalid Release: '{Title}' from indexer: {Indexer}. No Download URL provided.", release.Title, release.Indexer);
                return false;
            }

            return true;
        }

        protected virtual bool IsFullPage(IList<ReleaseInfo> page)
        {
            return PageSize != 0 && page.Count >= PageSize;
        }

        protected virtual IList<ReleaseInfo> FetchPage(IndexerRequest request, IParseIndexerResponse parser)
        {
            var response = FetchIndexerResponse(request);

            try
            {
                return parser.ParseResponse(response).ToList();
            }
            catch (Exception ex)
            {
                ex.WithData(response.HttpResponse, 128*1024);
                _logger.LogTrace("Unexpected Response content ({Length} bytes): {Content}", response.HttpResponse.ResponseData.Length, response.HttpResponse.Content);
                throw;
            }
        }

        protected virtual IndexerResponse FetchIndexerResponse(IndexerRequest request)
        {
            _logger.LogDebug("Downloading Feed {HttpRequestString}", request.HttpRequest.ToString(false));

            if (request.HttpRequest.RateLimit < RateLimit)
            {
                request.HttpRequest.RateLimit = RateLimit;
            }
            request.HttpRequest.RateLimitKey = Definition.Id.ToString();

            return new IndexerResponse(request, _httpClient.Execute(request.HttpRequest));
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
                var firstRequest = generator.GetRecentRequests().GetAllTiers().FirstOrDefault()?.FirstOrDefault();

                if (firstRequest == null)
                {
                    return new ValidationFailure(string.Empty, "No rss feed query available. This may be an issue with the indexer or your indexer category settings.");
                }

                var releases = FetchPage(firstRequest, parser);

                if (releases.Empty())
                {
                    return new ValidationFailure(string.Empty, "Query successful, but no results were returned from your indexer. This may be an issue with the indexer or your indexer category settings.");
                }
            }
            catch (ApiKeyException ex)
            {
                _logger.LogWarning("Indexer returned result for RSS URL, API Key appears to be invalid: {Message}", ex.Message);

                return new ValidationFailure("ApiKey", "Invalid API Key");
            }
            catch (RequestLimitReachedException ex)
            {
                _logger.LogWarning("Request limit reached: {Message}", ex.Message);
            }
            catch (CloudFlareCaptchaException ex)
            {
                if (ex.IsExpired)
                {
                    return new ValidationFailure("CaptchaToken", "CloudFlare CAPTCHA token expired, please Refresh.");
                }
                else
                {
                    return new ValidationFailure("CaptchaToken", "Site protected by CloudFlare CAPTCHA. Valid CAPTCHA token required.");
                }
            }
            catch (UnsupportedFeedException ex)
            {
                _logger.LogWarning(ex, "Indexer feed is not supported");

                return new ValidationFailure(string.Empty, "Indexer feed is not supported: {Message}", ex.Message);
            }
            catch (IndexerException ex)
            {
                _logger.LogWarning(ex, "Unable to connect to indexer");

                return new ValidationFailure(string.Empty, "Unable to connect to indexer. {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to connect to indexer");

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log for more details");
            }

            return null;
        }
    }

}
