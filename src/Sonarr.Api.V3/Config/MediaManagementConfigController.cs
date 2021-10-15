using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Config
{
    [ApiController]
    [SonarrApiConfigRoute("mediamanagement", RouteVersion.V3)]
    public class MediaManagementConfigController : SonarrConfigController<MediaManagementConfigResource>
    {
        public MediaManagementConfigController(IConfigService configService, PathExistsValidator pathExistsValidator, FolderChmodValidator folderChmodValidator)
            : base(configService)
        {
            /*SharedValidator.RuleFor(c => c.RecycleBinCleanupDays).GreaterThanOrEqualTo(0);
            SharedValidator.RuleFor(c => c.ChmodFolder).SetValidator(folderChmodValidator).When(c => !string.IsNullOrEmpty(c.ChmodFolder) && PlatformInfo.IsMono);
            SharedValidator.RuleFor(c => c.RecycleBin).IsValidPath().SetValidator(pathExistsValidator).When(c => !string.IsNullOrWhiteSpace(c.RecycleBin));
            SharedValidator.RuleFor(c => c.MinimumFreeSpaceWhenImporting).GreaterThanOrEqualTo(100);*/
        }

        protected override MediaManagementConfigResource ToResource(IConfigService model)
            => MediaManagementConfigResourceMapper.ToResource(model);
    }
}