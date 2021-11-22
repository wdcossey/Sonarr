using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Update
{
    public interface IUpdaterConfigProvider
    {

    }

    public class UpdaterConfigProvider : IUpdaterConfigProvider, IHandleAsync<ApplicationStartedEvent>
    {
        private ILogger<UpdaterConfigProvider> _logger;
        private IConfigFileProvider _configFileProvider;
        private IDeploymentInfoProvider _deploymentInfoProvider;

        public UpdaterConfigProvider(IDeploymentInfoProvider deploymentInfoProvider, IConfigFileProvider configFileProvider, ILogger<UpdaterConfigProvider> logger)
        {
            _deploymentInfoProvider = deploymentInfoProvider;
            _configFileProvider = configFileProvider;
            _logger = logger;
        }

        public Task HandleAsync(ApplicationStartedEvent message)
        {
            var updateMechanism = _configFileProvider.UpdateMechanism;
            var packageUpdateMechanism = _deploymentInfoProvider.PackageUpdateMechanism;

            var externalMechanisms = Enum.GetValues(typeof(UpdateMechanism))
                                         .Cast<UpdateMechanism>()
                                         .Where(v => v >= UpdateMechanism.External)
                                         .ToArray();

            foreach (var externalMechanism in externalMechanisms)
            {
                if (packageUpdateMechanism != externalMechanism && updateMechanism == externalMechanism ||
                    packageUpdateMechanism == externalMechanism && updateMechanism == UpdateMechanism.BuiltIn)
                {
                    _logger.LogInformation("Update mechanism {UpdateMechanism} not supported in the current configuration, changing to {PackageUpdateMechanism}.", updateMechanism, packageUpdateMechanism);
                    ChangeUpdateMechanism(packageUpdateMechanism);
                    break;
                }
            }

            if (_deploymentInfoProvider.IsExternalUpdateMechanism)
            {
                var currentBranch = _configFileProvider.Branch;
                var packageBranch = _deploymentInfoProvider.PackageBranch;
                if (packageBranch.IsNotNullOrWhiteSpace() & packageBranch != currentBranch)
                {
                    _logger.LogInformation("External updater uses branch {PackageBranch} instead of the currently selected {CurrentBranch}, changing to {ToPackageBranch}.", packageBranch, currentBranch, packageBranch);
                    ChangeBranch(packageBranch);
                }
            }
            
            return Task.CompletedTask;
        }

        private void ChangeUpdateMechanism(UpdateMechanism updateMechanism)
        {
            var config = new Dictionary<string, object>
            {
                [nameof(_configFileProvider.UpdateMechanism)] = updateMechanism
            };
            _configFileProvider.SaveConfigDictionary(config);
        }

        private void ChangeBranch(string branch)
        {
            var config = new Dictionary<string, object>
            {
                [nameof(_configFileProvider.Branch)] = branch
            };
            _configFileProvider.SaveConfigDictionary(config);
        }
    }
}
