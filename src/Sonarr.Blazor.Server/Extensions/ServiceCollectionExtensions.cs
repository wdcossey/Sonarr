using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using FluentMigrator;
using Microsoft.AspNetCore.Builder;
using NLog.Targets;
using NzbDrone.Common.Composition;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Messaging;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Notifications.Emby;
using Sonarr.Blazor.Server;


// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {

        public static IApplicationBuilder UseSonarr(this IApplicationBuilder app)
        {
            //var container = MainAppContainerBuilder.BuildContainer(startupContext);
            app.ApplicationServices.GetRequiredService<InitializeLogger>().Initialize();
            app.ApplicationServices.GetRequiredService<IAppFolderFactory>().Register();
            app.ApplicationServices.GetRequiredService<IProvidePidFile>().Write();

            //DbFactory.RegisterDatabase(container);

            return app;
        }

        public static IServiceCollection AddSonarr(this IServiceCollection services)
        {
            var assemblies = new List<string>
            {
                "Sonarr.Host.dll",
                "Sonarr.Core.dll",
                "Sonarr.Api.dll",
                "Sonarr.SignalR.dll",
                "Sonarr.Api.V3.dll",
                "Sonarr.Http.dll"
            };

            return services?.AddSonarr(assemblies);
        }

        public static IServiceCollection AddSonarr(this IServiceCollection services, IList<string> assemblyNames)
        {
            var loadedTypes = new List<Type>();

            assemblyNames.Add(OsInfo.IsWindows ? "Sonarr.Windows.dll" : "Sonarr.Mono.dll");
            assemblyNames.Add("Sonarr.Common.dll");
            
            foreach (var assemblyName in assemblyNames)
            {
                loadedTypes.AddRange(Assembly.LoadFrom(Path.Join(AppDomain.CurrentDomain.BaseDirectory, assemblyName)).GetExportedTypes());
            }
            
            loadedTypes.AutoRegisterInterfaces(services);

            services.Register<Command>(loadedTypes); //TODO: This is temporary for `Command` (need to register the Type, Factory?)
            services.AddSingleton<InitializeLogger>();
            services.AddSingleton<MediaBrowserProxy>();
            services.AddSingleton<SameEpisodesSpecification>();

            services.RegisterDatabase();
            //container.Resolve<DatabaseTarget>().Register();

            return services;
        }

        private static IServiceCollection RegisterDatabase(this IServiceCollection services)
        {
            services.AddSingleton<IMainDatabase>(provider =>
                new MainDatabase(provider.GetRequiredService<IDbFactory>().Create()));

            services.AddSingleton<ILogDatabase>(provider =>
                new LogDatabase(provider.GetRequiredService<IDbFactory>().Create(MigrationType.Log)));

            return services;
        }

        private static void AutoRegisterInterfaces(this IReadOnlyList<Type> loadedTypes, IServiceCollection services)
        {
            var loadedInterfaces = loadedTypes.Where(t => t.IsInterface).ToList();
            var implementedInterfaces = loadedTypes.SelectMany(t => t.GetInterfaces());

            var contracts = loadedInterfaces.Union(implementedInterfaces).Where(c =>
                    !c.IsGenericTypeDefinition && !string.IsNullOrWhiteSpace(c.FullName))
                .Where(c => !c.FullName.StartsWith("System"))
                //.Where(c => !c.FullName.EndsWith("Module"))
                //.Where(c => !c.FullName.EndsWith("Controller"))
                .Except(new List<Type> { typeof(IMessage), typeof(IEvent), typeof(IContainer) }).Distinct() //TODO: remove `IContainer`
                .OrderBy(c => c.FullName);

            var implementations = new List<ServiceDescriptor>();

            foreach (var contract in contracts)
                implementations.AddRange(AutoRegisterImplementations(contract, loadedTypes));

            foreach (var descriptor in implementations)
                services.Add(descriptor);
        }

        private static IServiceCollection Register<TService>(this IServiceCollection services, IEnumerable<Type> loadedTypes, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
        {
            var contracts = loadedTypes.Where(t => typeof(TService).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            var implementations = new List<ServiceDescriptor>();

            foreach (var contract in contracts)
                implementations.AddRange(Register(typeof(TService), contract, lifetime));

            foreach (var descriptor in implementations)
                services.Add(descriptor);

            return services;
        }

        private static IEnumerable<ServiceDescriptor> AutoRegisterImplementations(Type contractType, IEnumerable<Type> loadedTypes)
        {
            var implementations = contractType.GetImplementations(loadedTypes).Where(c => !c.IsGenericTypeDefinition).ToList();

            if (implementations.Count == 0)
                return Array.Empty<ServiceDescriptor>();

            return implementations.Count > 1
                ? contractType.RegisterAll(implementations, ServiceLifetime.Singleton)
                : contractType.Register(implementations.Single(), ServiceLifetime.Singleton);
        }

        private static IEnumerable<Type> GetImplementations(this Type contractType, IEnumerable<Type> loadedTypes)
        {
            return loadedTypes
                .Where(implementation =>
                    contractType.IsAssignableFrom(implementation) &&
                    !implementation.IsInterface &&
                    !implementation.IsAbstract
                );
        }

        private static IEnumerable<ServiceDescriptor> Register(this Type service, Type implementation, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            return new []
            {
                ServiceDescriptor.Describe(implementation, implementation, ServiceLifetime.Singleton),
                ServiceDescriptor.Describe(service, provider => provider.GetRequiredService(implementation), lifetime)
            };
        }

        private static IEnumerable<ServiceDescriptor> RegisterAll(this Type service, IEnumerable<Type> implementationList, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            var result = new List<ServiceDescriptor>();

            foreach (var implementation in implementationList)
                result.AddRange(Register(service, implementation, lifetime));

            return result;
        }
    }
}
