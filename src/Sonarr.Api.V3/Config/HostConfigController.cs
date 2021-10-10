using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;

namespace Sonarr.Api.V3.Config
{
    [ApiController]
    [SonarrV3ConfigRoute("host")]
    public class HostConfigController : SonarrControllerBase<HostConfigResource> // SonarrRestModule<HostConfigResource>
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IConfigService _configService;
        private readonly IUserService _userService;

        public HostConfigController(IConfigFileProvider configFileProvider, IConfigService configService, IUserService userService)
            //: base("/config/host")
        {
            _configFileProvider = configFileProvider;
            _configService = configService;
            _userService = userService;

            /*GetResourceSingle = GetHostConfig;
            GetResourceById = GetHostConfig;
            UpdateResource = SaveHostConfig;

            SharedValidator.RuleFor(c => c.BindAddress)
                           .ValidIp4Address()
                           .NotListenAllIp4Address()
                           .When(c => c.BindAddress != "*");

            SharedValidator.RuleFor(c => c.Port).ValidPort();

            SharedValidator.RuleFor(c => c.UrlBase).ValidUrlBase();

            SharedValidator.RuleFor(c => c.Username).NotEmpty().When(c => c.AuthenticationMethod != AuthenticationType.None);
            SharedValidator.RuleFor(c => c.Password).NotEmpty().When(c => c.AuthenticationMethod != AuthenticationType.None);

            SharedValidator.RuleFor(c => c.SslPort).ValidPort().When(c => c.EnableSsl);
            SharedValidator.RuleFor(c => c.SslPort).NotEqual(c => c.Port).When(c => c.EnableSsl);
            SharedValidator.RuleFor(c => c.SslCertHash).NotEmpty().When(c => c.EnableSsl && OsInfo.IsWindows);

            SharedValidator.RuleFor(c => c.Branch).NotEmpty().WithMessage("Branch name is required, 'master' is the default");
            SharedValidator.RuleFor(c => c.UpdateScriptPath).IsValidPath().When(c => c.UpdateMechanism == UpdateMechanism.Script);

            SharedValidator.RuleFor(c => c.BackupFolder).IsValidPath().When(c => Path.IsPathRooted(c.BackupFolder));
            SharedValidator.RuleFor(c => c.BackupInterval).InclusiveBetween(1, 7);
            SharedValidator.RuleFor(c => c.BackupRetention).InclusiveBetween(1, 90);*/

        }

        public override Task<IActionResult> GetAllAsync()
            => Task.FromResult<IActionResult>(Ok(GetHostConfig()));

        protected override Task<IList<HostConfigResource>> GetAllResourcesAsync()
            => throw new NotImplementedException();

        protected override Task<HostConfigResource> GetResourceByIdAsync(int id)
            => Task.FromResult(GetHostConfig());

        protected override Task DeleteResourceByIdAsync(int id)
            => throw new NotImplementedException();

        protected override Task<HostConfigResource> UpdateResourceAsync(HostConfigResource resource)
        {
            var dictionary = resource.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(resource, null));

            _configFileProvider.SaveConfigDictionary(dictionary);
            _configService.SaveConfigDictionary(dictionary);

            if (resource.Username.IsNotNullOrWhiteSpace() && resource.Password.IsNotNullOrWhiteSpace())
            {
                _userService.Upsert(resource.Username, resource.Password);
            }

            return Task.FromResult(GetHostConfig());
        }

        protected override Task<HostConfigResource> CreateResourceAsync(HostConfigResource resource)
            => throw new NotImplementedException();

        private HostConfigResource GetHostConfig()
        {
            var resource = _configFileProvider.ToResource(_configService);
            resource.Id = 1;

            var user = _userService.FindUser();
            if (user != null)
            {
                resource.Username = user.Username;
                resource.Password = user.Password;
            }

            return resource;
        }
    }
}
