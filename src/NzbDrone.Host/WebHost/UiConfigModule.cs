using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NzbDrone.Api.Config;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Host.WebHost
{
    public class UiConfigModule : WebApiController
    {
        private readonly IConfigService _configService;

        public UiConfigModule(IConfigService configService)
            //: base(configService)
        {
            _configService = configService;
        }

        [Route(HttpVerbs.Get, "/ui")]
        public Task<UiConfigResource> GetStatusAsync()
        {
            return Task.FromResult<UiConfigResource>(new UiConfigResource
            {
                FirstDayOfWeek = _configService.FirstDayOfWeek,
                CalendarWeekColumnHeader = _configService.CalendarWeekColumnHeader,

                ShortDateFormat = _configService.ShortDateFormat,
                LongDateFormat = _configService.LongDateFormat,
                TimeFormat = _configService.TimeFormat,
                ShowRelativeDates = _configService.ShowRelativeDates,

                EnableColorImpairedMode = _configService.EnableColorImpairedMode,
            });
        }
    }
}
