using System.Linq;
using NLog;
using NLog.Fluent;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Instrumentation.Sentry;

namespace NzbDrone.Common.Instrumentation
{
    public class InitializeLogger
    {
        private readonly IOsInfo _osInfo;

        public InitializeLogger(IOsInfo osInfo)
        {
            _osInfo = osInfo;
        }

        public void Initialize()
        {
            //TODO: Skip this until NLog is implemented via `Microsoft.Extensions.DependencyInjection`
            SentryTarget sentryTarget = null;//LogManager.Configuration.AllTargets.OfType<SentryTarget>().FirstOrDefault();
            if (sentryTarget != null)
            {
                sentryTarget.UpdateScope(_osInfo);
            }
        }
    }
}
