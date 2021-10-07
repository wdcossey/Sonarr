using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Files;
using EmbedIO.Utilities;
using EmbedIO.WebApi;
using NzbDrone.Common.Composition;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Analytics;
using NzbDrone.Core.Configuration;
using NzbDrone.Host.AccessControl;
using NzbDrone.Host.WebHost.DiskSpace;
using NzbDrone.Host.WebHost.Health;
using NzbDrone.Host.WebHost.Language;
using NzbDrone.Host.WebHost.Logs;
using NzbDrone.Host.WebHost.MediaCovers;
using NzbDrone.Host.WebHost.Quality;
using NzbDrone.Host.WebHost.Queue;
using NzbDrone.Host.WebHost.RootFolders;
using NzbDrone.Host.WebHost.Series;
using NzbDrone.Host.WebHost.System;
using Swan.Logging;
using Diagnostics = System.Diagnostics;

namespace NzbDrone.Host.WebHost
{
    public class HostController : IHostController
    {
        private readonly IContainer _container;
        private readonly IRemoteAccessAdapter _remoteAccessAdapter;
        private readonly IUrlAclAdapter _urlAclAdapter;
        //private static IConfigFileProvider _configFileProvider;
        //private static IAnalyticsService _analyticsService;
        private static string _generatedContent;

        public HostController(
            IContainer container,
            //IOwinAppFactory owinAppFactory,
            IRemoteAccessAdapter remoteAccessAdapter,
            IUrlAclAdapter urlAclAdapter
            /*Logger logger*/)
        {
            //_owinAppFactory = owinAppFactory;
            _container = container;
            _remoteAccessAdapter = remoteAccessAdapter;
            _urlAclAdapter = urlAclAdapter;
            //_configFileProvider = configFileProvider;
            //_analyticsService = analyticsService;
            //_logger = logger;
        }

        public void StartServer()
        {

            /*Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });*/

            // Our web server is disposable.
            var server = CreateWebServer("http://localhost:9696/");
            {
                // Once we've registered our modules and configured them, we call the RunAsync() method.
                server.RunAsync();

                var browser = new Diagnostics.Process()
                {
                    StartInfo = new Diagnostics.ProcessStartInfo("http://localhost:9696/") { UseShellExecute = true }
                };
                browser.Start();
                // Wait for any key to be pressed before disposing of our web server.
                // In a service, we'd manage the lifecycle of our web server using
                // something like a BackgroundWorker or a ManualResetEvent.
                //Console.ReadKey(true);
            }

            _remoteAccessAdapter.MakeAccessible(true);

            //_logger.Info("Listening on the following URLs:");
            foreach (var url in _urlAclAdapter.Urls)
            {
                //_logger.Info("  {0}", url);
            }

            //_owinApp = _owinAppFactory.CreateApp(_urlAclAdapter.Urls);
        }

        // Create and configure our web server.
        private WebServer CreateWebServer(string url)
        {
            var server = new WebServer(o => o
                    .WithUrlPrefix(url)
                    .WithMode(HttpListenerMode.EmbedIO))
                // First, we will configure our web server by adding Modules.
                .WithLocalSessionManager()
                .WithWebApi("/api/v3/system", Serializer, m => m
                    .RegisterController<SystemModule>(() => _container.Resolve<SystemModule>()))
                .WithWebApi("/api/v3/config", Serializer, m => m
                    .RegisterController<UiConfigModule>(() => _container.Resolve<UiConfigModule>()))
                .WithWebApi("/api/v3/series/editor", Serializer, m => m
                    .RegisterController<SeriesEditorModule>(() => _container.Resolve<SeriesEditorModule>()))
                .WithWebApi("/api/v3/series", Serializer, m => m
                    .RegisterController<SeriesModule>(() => _container.Resolve<SeriesModule>()))
                .WithWebApi("/api/v3/qualityprofile", Serializer, m => m
                    .RegisterController<QualityProfileModule>(() => _container.Resolve<QualityProfileModule>()))
                .WithWebApi("/api/v3/languageprofile", Serializer, m => m
                    .RegisterController<LanguageProfileModule>(() => _container.Resolve<LanguageProfileModule>()))
                .WithWebApi("/api/v3/health", Serializer, m => m
                    .RegisterController<HealthModule>(() => _container.Resolve<HealthModule>()))
                .WithWebApi("/api/v3/queue/status", Serializer, m => m
                    .RegisterController<QueueStatusModule>(() => _container.Resolve<QueueStatusModule>()))
                .WithWebApi("/api/v3/rootFolder", Serializer, m => m
                    .RegisterController<RootFolderModule>(() => _container.Resolve<RootFolderModule>()))
                .WithWebApi("/api/v3/diskspace", Serializer, m => m
                    .RegisterController<DiskSpaceModule>(() => _container.Resolve<DiskSpaceModule>()))
                .WithWebApi("/api/v3/log/file", Serializer, m => m
                    .RegisterController<LogFileModule>(() => _container.Resolve<LogFileModule>()))
                .WithWebApi("/api/v3/log", Serializer, m => m
                    .RegisterController<LogModule>(() => _container.Resolve<LogModule>()))
                //.WithWebApi("/api", m => m
                //    .WithController<PeopleController>())
                //.WithModule(new WebSocketChatModule("/chat"))

                .WithModule(_container.Resolve<InitializeJsModule>())
                .WithWebApi("/MediaCover", m => m.RegisterController<MediaCoverModule>(() => _container.Resolve<MediaCoverModule>()))
                .WithAction("/api/v3/customFilter", HttpVerbs.Get, context => context.SendStringAsync("[]", "application/json", Encoding.UTF8) )
                .WithAction("/api/v3/tag", HttpVerbs.Get, context => context.SendStringAsync("[]", "application/json", Encoding.UTF8) )
                .WithAction("/api/v3/importlist", HttpVerbs.Get, context => context.SendStringAsync("[]", "application/json", Encoding.UTF8) )
                //.WithAction("/api/v3/system/status", HttpVerbs.Get, context => context.SendStringAsync("bleh", "application/json", Encoding.UTF8) )
                //.WithModule(_container.Resolve<SystemModule>())
                //.WithModule(_container.Resolve<WebSocketsServer>())
                .WithStaticFolder("/", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI"), true, m => m.WithContentCaching(true)) // Add static files after other modules to avoid conflicts
                .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Error" })));

            // Listen for state changes.
            server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();

            return server;
        }

        private async Task Serializer(IHttpContext context, object? data)
        {
            Validate.NotNull(nameof(context), context).Response.ContentType = MimeType.Json;
            using var text = context.OpenResponseText(new UTF8Encoding(false));
            await text.WriteAsync(JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
                IgnoreNullValues = true
            } )).ConfigureAwait(false);
        }

        public void StopServer()
        {
            //if (_owinApp == null) return;

            //_logger.Info("Attempting to stop OWIN host");
            //_owinApp.Dispose();
            //_owinApp = null;
            //_logger.Info("Host has stopped");
        }
    }
}
