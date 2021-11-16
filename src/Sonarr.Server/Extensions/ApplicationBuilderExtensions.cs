using Microsoft.AspNetCore.Builder;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Processes;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSonarrServices(this IApplicationBuilder app)
        {
            //var container = MainAppContainerBuilder.BuildContainer(startupContext);
            app.ApplicationServices.GetRequiredService<InitializeLogger>().Initialize();
            app.ApplicationServices.GetRequiredService<IAppFolderFactory>().Register();
            app.ApplicationServices.GetRequiredService<IProvidePidFile>().Write();

            return app;
        }
    }
}
