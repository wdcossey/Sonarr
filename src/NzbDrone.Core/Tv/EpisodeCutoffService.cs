﻿using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Profiles.Languages;

namespace NzbDrone.Core.Tv
{
    public interface IEpisodeCutoffService
    {
        PagingSpec<Episode> EpisodesWhereCutoffUnmet(PagingSpec<Episode> pagingSpec);
    }

    public class EpisodeCutoffService : IEpisodeCutoffService
    {
        private readonly IEpisodeRepository _episodeRepository;
        private readonly IQualityProfileService _qualityProfileService;
        private readonly ILanguageProfileService _languageProfileService;

        public EpisodeCutoffService(IEpisodeRepository episodeRepository, IQualityProfileService qualityProfileService, ILanguageProfileService languageProfileService)
        {
            _episodeRepository = episodeRepository;
            _qualityProfileService = qualityProfileService;
            _languageProfileService = languageProfileService;
        }

        public PagingSpec<Episode> EpisodesWhereCutoffUnmet(PagingSpec<Episode> pagingSpec)
        {
            var qualitiesBelowCutoff = new List<QualitiesBelowCutoff>();
            var languagesBelowCutoff = new List<LanguagesBelowCutoff>();
            var profiles = _qualityProfileService.All();
            var languageProfiles = _languageProfileService.All();

            //Get all items less than the cutoff
            foreach (var profile in profiles)
            {
                var cutoffIndex = profile.GetIndex(profile.Cutoff);
                var belowCutoff = profile.Items.Take(cutoffIndex.Index).ToList();

                if (belowCutoff.Any())
                {
                    qualitiesBelowCutoff.Add(new QualitiesBelowCutoff(profile.Id, belowCutoff.SelectMany(i => i.GetQualities().Select(q => q.Id))));
                }
            }

            foreach (var profile in languageProfiles)
            {
                var languageCutoffIndex = profile.Languages.FindIndex(v => v.Language == profile.Cutoff);
                var belowLanguageCutoff = profile.Languages.Take(languageCutoffIndex).ToList();

                if (belowLanguageCutoff.Any())
                {
                    languagesBelowCutoff.Add(new LanguagesBelowCutoff(profile.Id, belowLanguageCutoff.Select(l => l.Language.Id)));
                }
            }

            return _episodeRepository.EpisodesWhereCutoffUnmet(pagingSpec, qualitiesBelowCutoff, languagesBelowCutoff, false);
        }
    }
}
