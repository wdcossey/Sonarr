using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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
using NzbDrone.Core.Notifications.Emby;


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

        public static IServiceCollection AddSonarr(this IServiceCollection services, IList<string> assemblies)
        {
            var loadedTypes = new List<Type>();

            assemblies.Add(OsInfo.IsWindows ? "Sonarr.Windows.dll" : "Sonarr.Mono.dll");
            assemblies.Add("Sonarr.Common.dll");

            foreach (var assembly in assemblies)
            {
                loadedTypes.AddRange(Assembly.LoadFrom(Path.Join(AppDomain.CurrentDomain.BaseDirectory, assembly)).GetExportedTypes());
            }

            //Container = new Container(new TinyIoCContainer(), _loadedTypes);
            loadedTypes.AutoRegisterInterfaces(services);
            //Container.Register(args);

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

        private static IEnumerable<ServiceDescriptor> AutoRegisterImplementations(Type contractType, IReadOnlyList<Type> loadedTypes)
        {
            var implementations = contractType.GetImplementations(loadedTypes).Where(c => !c.IsGenericTypeDefinition).ToList();

            if (implementations.Count == 0)
                return Array.Empty<ServiceDescriptor>();

            return implementations.Count > 1
                ? contractType.RegisterAllAsSingleton(implementations)
                : contractType.RegisterSingleton(implementations.Single());
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

        private static IEnumerable<ServiceDescriptor> RegisterSingleton(this Type service, Type implementation)
        {
            //Console.WriteLine($"DependencyInjection: {nameof(RegisterSingleton)} ==> {nameof(service)}: {service.FullName}, {nameof(implementation)}: {implementation.FullName}");

            return new []
            {
                ServiceDescriptor.Describe(implementation, implementation, ServiceLifetime.Singleton),
                ServiceDescriptor.Describe(service, provider => provider.GetRequiredService(implementation), ServiceLifetime.Singleton)
            };
            /*var factory = CreateSingletonImplementationFactory(implementation);

            // For Resolve and ResolveAll
            _container.Register(service, factory);

            // For ctor(IEnumerable<T>)
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(service);

            Console.WriteLine($"DependencyInjection: {nameof(RegisterSingleton)} ==> {nameof(service)}: {service.FullName}, {nameof(implementation)}: {implementation.FullName}, {nameof(enumerableType)}: {enumerableType.FullName}");

            _container.Register(enumerableType, (c, p) =>
            {
                var instance = factory(c, p);
                var result = Array.CreateInstance(service, 1);
                result.SetValue(instance, 0);
                return result;
            });*/
        }

        private static IEnumerable<ServiceDescriptor> RegisterAllAsSingleton(this Type service, IEnumerable<Type> implementationList)
        {
            var result = new List<ServiceDescriptor>();

            foreach (var implementation in implementationList)
            {
                //var factory = CreateSingletonImplementationFactory(implementation);

                //Console.WriteLine($"DependencyInjection: {nameof(RegisterAllAsSingleton)} ==> {nameof(service)}: {service.FullName}, {nameof(implementation)}: {implementation.FullName}");
                //yield return ServiceDescriptor.Singleton(service, implementation);

                result.AddRange(RegisterSingleton(service, implementation));

                // For ResolveAll and ctor(IEnumerable<T>)
                //_container.Register(service, factory, implementation.FullName);
            }

            return result;
        }

        /*private static Func<TinyIoCContainer, NamedParameterOverloads, object> CreateSingletonImplementationFactory(Type implementation)
        {
            const string singleImplPrefix = "singleImpl_";

            _container.Register(implementation, implementation, singleImplPrefix + implementation.FullName).AsSingleton();

            return (c, p) => _container.Resolve(implementation, singleImplPrefix + implementation.FullName);
        }*/
    }
}
