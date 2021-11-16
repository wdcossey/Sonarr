using System;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Core.HealthCheck.Checks
{
    public class DotnetVersionCheck : HealthCheckBase
    {
        private readonly IPlatformInfo _platformInfo;
        private readonly IOsInfo _osInfo;
        private readonly ILogger<DotnetVersionCheck> _logger;

        public DotnetVersionCheck(IPlatformInfo platformInfo, IOsInfo osInfo, ILogger<DotnetVersionCheck> logger)
        {
            _platformInfo = platformInfo;
            _osInfo = osInfo;
            _logger = logger;
        }

        public override HealthCheck Check()
        {
            var dotnetVersion = _platformInfo.Version;

            // Target .Net version, which would allow us to increase our target framework
            var targetVersion = new Version("6.0"); //TODO: .Net 6
            if (dotnetVersion >= targetVersion)
            {
                _logger.LogDebug("Dotnet version is {TargetVersion} or better: {DotnetVersion}", targetVersion, dotnetVersion);
                return new HealthCheck(GetType());
            }

            // Supported .net version but below our desired target
            var stableVersion = new Version("4.7.2"); //TODO: .Net 6
            if (dotnetVersion >= stableVersion)
            {
                _logger.LogDebug("Dotnet version is {StableVersion} or better: {DotnetVersion}", stableVersion, dotnetVersion);
                return new HealthCheck(GetType(), HealthCheckResult.Notice,
                    $"Currently installed .Net Runtime {dotnetVersion} is supported but we recommend upgrading to at least {targetVersion}.",
                    "#currently-installed-net-framework-is-supported-but-upgrading-is-recommended");
            }

            if (Version.TryParse(_osInfo.Version, out var osVersion) && osVersion < new Version("10.0.14393"))
            {
                return new HealthCheck(GetType(), HealthCheckResult.Error,
                    $"Currently installed .Net Runtime {dotnetVersion} is no longer supported. However your Operating System cannot be upgraded to {targetVersion}.",
                    "#currently-installed-net-framework-is-old-and-unsupported");
            }

            var oldVersion = new Version("4.6.2"); //TODO: .Net 6
            if (dotnetVersion >= oldVersion)
            {
                return new HealthCheck(GetType(), HealthCheckResult.Error,
                    $"Currently installed .Net Runtime {dotnetVersion} is no longer supported. Please upgrade the .Net Runtime to at least {targetVersion}.",
                    "#currently-installed-net-framework-is-old-and-unsupported");
            }

            return new HealthCheck(GetType(), HealthCheckResult.Error,
                $"Currently installed .Net Runtime {dotnetVersion} is old and unsupported. Please upgrade the .Net Runtime to at least {targetVersion}.",
                "#currently-installed-net-framework-is-old-and-unsupported");
        }

        public override bool CheckOnSchedule => false;
    }
}
