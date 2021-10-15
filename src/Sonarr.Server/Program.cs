using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation;

namespace Sonarr.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var startupArgs = new StartupContext(args);
            try
            {
                NzbDroneLogger.Register(startupArgs, false, true);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("NLog Exception: " + ex.ToString());
                throw;
            }

            return Host
                .CreateDefaultBuilder(args)
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseWebRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI"))
                        .UseStartup<Startup>()
                        .UseKestrel(options => options.AddServerHeader = false)
                        .ConfigureServices(services =>
                            services.AddSingleton<IStartupContext>(provider => new StartupContext(args)))
                        .UseDefaultServiceProvider(options => options.ValidateOnBuild = false);
                });
        }
    }
}
