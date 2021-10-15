using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net;
using Ical.Net.DataTypes;
using Ical.Net.General;
using Ical.Net.Interfaces.Serialization;
using Ical.Net.Serialization;
using Ical.Net.Serialization.iCalendar.Factory;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Tags;
using NzbDrone.Core.Tv;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Calendar
{
    [ApiController]
    [SonarrFeedRoute("calendar", RouteVersion.V3)]
    public class CalendarFeedController : ControllerBase
    {
        private readonly IEpisodeService _episodeService;
        private readonly ITagService _tagService;

        public CalendarFeedController(IEpisodeService episodeService, ITagService tagService)
        {
            _episodeService = episodeService;
            _tagService = tagService;
        }

        /// <summary>
        /// There was a typo, recognize both the correct 'premieresOnly' and mistyped 'premiersOnly' boolean for background compat.
        /// </summary>
        [HttpGet("Sonarr.ics")]
        public IActionResult GetCalendarFeed(
            [FromQuery] bool unmonitored = false,
            [FromQuery] bool? premieresOnly = false,
            [FromQuery] bool? premiersOnly = false,
            [FromQuery] bool asAllDay = false,
            [FromQuery] int pastDays = 7,
            [FromQuery] int futureDays = 28,
            [FromQuery] IList<int> tags = null)
        {
            var start = DateTime.Today.AddDays(-pastDays);
            var end = DateTime.Today.AddDays(futureDays);

            var episodes = _episodeService.EpisodesBetweenDates(start, end, unmonitored);
            var calendar = new Ical.Net.Calendar
            {
                ProductId = "-//sonarr.tv//Sonarr//EN"
            };

            var calendarName = "Sonarr TV Schedule";
            calendar.AddProperty(new CalendarProperty("NAME", calendarName));
            calendar.AddProperty(new CalendarProperty("X-WR-CALNAME", calendarName));

            foreach (var episode in episodes.OrderBy(v => v.AirDateUtc.Value))
            {
                if (((premieresOnly ?? false) || (premiersOnly ?? false)) && (episode.SeasonNumber == 0 || episode.EpisodeNumber != 1))
                    continue;

                if (tags?.Any() == true && tags.None(episode.Series.Tags.Contains))
                    continue;

                var occurrence = calendar.Create<Event>();
                occurrence.Uid = "NzbDrone_episode_" + episode.Id;
                occurrence.Status = episode.HasFile ? EventStatus.Confirmed : EventStatus.Tentative;
                occurrence.Description = episode.Overview;
                occurrence.Categories = new List<string>() { episode.Series.Network };

                if (asAllDay)
                {
                    occurrence.Start = new CalDateTime(episode.AirDateUtc.Value.ToLocalTime()) { HasTime = false };
                }
                else
                {
                    occurrence.Start = new CalDateTime(episode.AirDateUtc.Value) { HasTime = true };
                    occurrence.End = new CalDateTime(episode.AirDateUtc.Value.AddMinutes(episode.Series.Runtime))
                        { HasTime = true };
                }

                occurrence.Summary = episode.Series.SeriesType switch
                {
                    SeriesTypes.Daily => $"{episode.Series.Title} - {episode.Title}",
                    _ => $"{episode.Series.Title} - {episode.SeasonNumber}x{episode.EpisodeNumber:00} - {episode.Title}"
                };
            }

            var serializer =
                (IStringSerializer)new SerializerFactory().Build(calendar.GetType(), new SerializationContext());
            var icalendar = serializer.SerializeToString(calendar);

            return Content(icalendar, "text/calendar");
        }
    }
}
