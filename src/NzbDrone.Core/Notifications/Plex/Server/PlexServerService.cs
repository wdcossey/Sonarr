using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using FluentValidation.Results;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.Plex.Server
{
    public interface IPlexServerService
    {
        void UpdateLibrary(Series series, PlexServerSettings settings);
        void UpdateLibrary(IEnumerable<Series> series, PlexServerSettings settings);
        ValidationFailure Test(PlexServerSettings settings);
    }

    public class PlexServerService : IPlexServerService
    {
        private readonly ICached<Version> _versionCache;
        private readonly ICached<bool> _partialUpdateCache;
        private readonly IPlexServerProxy _plexServerProxy;
        private readonly ILogger<PlexServerService> _logger;

        public PlexServerService(ICacheManager cacheManager, IPlexServerProxy plexServerProxy, ILogger<PlexServerService> logger)
        {
            _versionCache = cacheManager.GetCache<Version>(GetType(), "versionCache");
            _partialUpdateCache = cacheManager.GetCache<bool>(GetType(), "partialUpdateCache");
            _plexServerProxy = plexServerProxy;
            _logger = logger;
        }

        public void UpdateLibrary(Series series, PlexServerSettings settings)
        {
            UpdateLibrary(new[] { series }, settings);
        }

        public void UpdateLibrary(IEnumerable<Series> multipleSeries, PlexServerSettings settings)
        {
            try
            {
                _logger.LogDebug("Sending Update Request to Plex Server");
                var watch = Stopwatch.StartNew();

                var version = _versionCache.Get(settings.Host, () => GetVersion(settings), TimeSpan.FromHours(2));
                ValidateVersion(version);

                var sections = GetSections(settings);
                var partialUpdates = _partialUpdateCache.Get(settings.Host, () => PartialUpdatesAllowed(settings, version), TimeSpan.FromHours(2));

                if (partialUpdates)
                {
                    var partiallyUpdated = true;

                    foreach (var series in multipleSeries)
                    {
                        partiallyUpdated &= UpdatePartialSection(series, sections, settings);

                        if (!partiallyUpdated)
                        {
                            break;
                        }
                    }

                    // Only update complete sections if all partial updates failed
                    if (!partiallyUpdated)
                    {
                        _logger.LogDebug("Unable to update partial section, updating all TV sections");
                        sections.ForEach(s => UpdateSection(s.Id, settings));
                    }
                }
                else
                {
                    sections.ForEach(s => UpdateSection(s.Id, settings));
                }

                _logger.LogDebug("Finished sending Update Request to Plex Server (took {ElapsedMilliseconds} ms)", watch.ElapsedMilliseconds);
            }

            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to Update Plex host: {Host}", settings.Host);
                throw;
            }
        }

        private List<PlexSection> GetSections(PlexServerSettings settings)
        {
            _logger.LogDebug("Getting sections from Plex host: {Host}", settings.Host);

            return _plexServerProxy.GetTvSections(settings).ToList();
        }

        private bool PartialUpdatesAllowed(PlexServerSettings settings, Version version)
        {
            try
            {
                if (version >= new Version(0, 9, 12, 0))
                {
                    var preferences = GetPreferences(settings);
                    var partialScanPreference = preferences.SingleOrDefault(p => p.Id.Equals("FSEventLibraryPartialScanEnabled"));

                    if (partialScanPreference == null)
                    {
                        return false;
                    }

                    return Convert.ToBoolean(partialScanPreference.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to check if partial updates are allowed");
            }

            return false;
        }

        private void ValidateVersion(Version version)
        {
            if (version >= new Version(1, 3, 0) && version < new Version(1, 3, 1))
            {
                throw new PlexVersionException("Found version {0}, upgrade to PMS 1.3.1 to fix library updating and then restart Sonarr", version);
            }
        }

        private Version GetVersion(PlexServerSettings settings)
        {
            _logger.LogDebug("Getting version from Plex host: {Host}", settings.Host);

            var rawVersion = _plexServerProxy.Version(settings);
            var version = new Version(Regex.Match(rawVersion, @"^(\d+[.-]){4}").Value.Trim('.', '-'));

            return version;
        }

        private List<PlexPreference> GetPreferences(PlexServerSettings settings)
        {
            _logger.LogDebug("Getting preferences from Plex host: {Host}", settings.Host);

            return _plexServerProxy.Preferences(settings);
        }

        private void UpdateSection(int sectionId, PlexServerSettings settings)
        {
            _logger.LogDebug("Updating Plex host: {Host}, Section: {SectionId}", settings.Host, sectionId);

            _plexServerProxy.Update(sectionId, settings);
        }

        private bool UpdatePartialSection(Series series, List<PlexSection> sections, PlexServerSettings settings)
        {
            var partiallyUpdated = false;

            foreach (var section in sections)
            {
                var metadataId = GetMetadataId(section.Id, series, section.Language, settings);

                if (metadataId.IsNotNullOrWhiteSpace())
                {
                    _logger.LogDebug("Updating Plex host: {Host}, Section: {SectionId}, Series: {Series}", settings.Host, section.Id, series);
                    _plexServerProxy.UpdateSeries(metadataId, settings);

                    partiallyUpdated = true;
                }
            }

            return partiallyUpdated;
        }

        private string GetMetadataId(int sectionId, Series series, string language, PlexServerSettings settings)
        {
            _logger.LogDebug("Getting metadata from Plex host: {Host} for series: {Series}", settings.Host, series);

            return _plexServerProxy.GetMetadataId(sectionId, series.TvdbId, language, settings);
        }

        public ValidationFailure Test(PlexServerSettings settings)
        {
            try
            {
                _versionCache.Remove(settings.Host);
                _partialUpdateCache.Remove(settings.Host);
                var sections = GetSections(settings);

                if (sections.Empty())
                {
                    return new ValidationFailure("Host", "At least one TV library is required");
                }
            }
            catch(PlexAuthenticationException ex)
            {
                _logger.LogError(ex, "Unable to connect to Plex Server");
                return new ValidationFailure("AuthToken", "Invalid authentication token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to connect to Plex Server");
                return new ValidationFailure("Host", "Unable to connect to Plex Server");
            }

            return null;
        }
    }
}
