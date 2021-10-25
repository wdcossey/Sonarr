using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using NzbDrone.Common.Serializer;
using Sonarr.Server.HostedServices;
using Sonarr.Server.Hubs;
using Sonarr.Server.Middleware;

namespace Sonarr.Server
{
    public class Startup
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
            => Configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            //TODO: Fix NLog integration!
            services.AddSingleton<NLog.Logger>(provider => NLog.LogManager.GetCurrentClassLogger());

            services.AddSonarr();

            services.AddResponseCompression();

            services
                .AddControllersWithViews()
                .AddApplicationPart(Assembly.Load(new AssemblyName("Sonarr.Http")))
                .AddApplicationPart(Assembly.Load(new AssemblyName("Sonarr.Api.V3")));

            services
                .AddRazorPages()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                    //options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    //options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                    options.JsonSerializerOptions.Converters.Add(new JsonVersionConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonHttpUriConverter());
                });

            services.AddResponseCaching();
            services.AddHostedService<SonarrHostedService>();
            services.AddSignalR();
            //services.AddAuthorization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler("/error");

            if (!env.IsDevelopment())
            {
                //app.UseHsts();
            }

            app.UseSonarr();

            //app.UseHttpsRedirection();

            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), "UI")),
                RequestPath = string.Empty
            });

            app.UseRouting();

            //app.UseAuthentication();
            //app.UseAuthorization();

            app.UseMiddleware<SonarrResponseHeaderMiddleware>();
            //app.UseMiddleware<ApiKeyAuthorizationMiddleware>();

            app.UseResponseCaching();
            app.UseResponseCompression();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<SonarrHub>("/signalr");

                endpoints.MapRazorPages();
                endpoints.MapControllers()/*.RequireAuthorization()*/;
                endpoints.MapFallbackToFile("index.html");
            });
        }

    }
}
