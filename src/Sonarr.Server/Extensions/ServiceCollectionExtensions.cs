using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NzbDrone.Common.Composition;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Notifications.Emby;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        private static readonly HashSet<Type> TransientTypes = new() {typeof(FluentValidation.IValidator), typeof(FluentValidation.Validators.IPropertyValidator)/*, typeof(IHttpClient<>)*/};

        public static IServiceCollection AddSonarrServices(this IServiceCollection services)
        {
            //TODO: Replace with file scanner *Sonar*.dll
            var assemblies = new List<string>
            {
                "Sonarr.Host.dll",
                "Sonarr.Core.dll",
                //"Sonarr.Api.dll",
                "Sonarr.SignalR.dll",
                "Sonarr.Api.V3.dll",
                "Sonarr.Http.dll"
            };

            return services.AddSonarrServices(assemblies);
        }

        public static IServiceCollection AddSonarrServices(this IServiceCollection services, IList<string> assemblyNames)
        {
            var loadedTypes = new List<Type>();

            assemblyNames.Add(OsInfo.IsWindows ? "Sonarr.Windows.dll" : "Sonarr.Mono.dll");
            assemblyNames.Add("Sonarr.Common.dll");

            foreach (var assemblyName in assemblyNames)
            {
                //Assembly.Load(new AssemblyName(assemblyName))
                //Assembly.LoadFrom("Sonarr.Api.dll")
                loadedTypes.AddRange(Assembly.LoadFrom(assemblyName).GetExportedTypes());
            }

            loadedTypes.AutoRegisterInterfaces(services);

            services.AddSingleton<InitializeLogger>();
            services.AddSingleton<SameEpisodesSpecification>();
            //services.AddSingleton<IHandle<ApplicationShutdownRequested>, ShutdownEventHandler>();
            
            services.AddTransient(typeof(IHttpClient<>), typeof(HttpClient<>));
            
            services.AddSonarrDatabases();

            return services;
        }

        private static IServiceCollection AddSonarrDatabases(this IServiceCollection services)
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

            var contractTypes = loadedInterfaces.Union(implementedInterfaces).Where(c =>
                    !c.IsGenericTypeDefinition && !string.IsNullOrWhiteSpace(c.FullName))
                .Where(c => !string.IsNullOrWhiteSpace(c.FullName) && !c.FullName.StartsWith("System"))
                .Except(new List<Type> { typeof(IMessage), typeof(IEvent), typeof(IContainer) }).Distinct() //TODO: remove `IContainer`
                .OrderBy(c => c.FullName);

            var implementations = new List<ServiceDescriptor>();
            
            foreach (var contractType in contractTypes)
                implementations.AddRange(AutoRegisterImplementations(contractType: contractType, loadedTypes: loadedTypes, lifetimeFunc: (lifetimeType) =>
                {
                    return TransientTypes.Any(a => a.IsAssignableFrom(lifetimeType)) 
                        ? ServiceLifetime.Transient 
                        : ServiceLifetime.Singleton;
                }));

            foreach (var descriptor in implementations)
                services.Add(descriptor);
        }

        private static IServiceCollection Register<TService>(this IServiceCollection services, IEnumerable<Type> loadedTypes, ServiceLifetime lifetime = ServiceLifetime.Transient)
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

        private static IEnumerable<ServiceDescriptor> AutoRegisterImplementations(Type contractType, IEnumerable<Type> loadedTypes, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            var implementations = contractType.GetImplementations(loadedTypes).Where(c => !c.IsGenericTypeDefinition).ToList();

            if (implementations.Count == 0)
                return Array.Empty<ServiceDescriptor>();

            return implementations.Count > 1
                ? contractType.RegisterAll(implementations, lifetime)
                : contractType.Register(implementations.Single(), lifetime);
        }

        private static IEnumerable<ServiceDescriptor> AutoRegisterImplementations(Type contractType, IEnumerable<Type> loadedTypes, Func<Type, ServiceLifetime> lifetimeFunc)
        {
            var implementations = contractType.GetImplementations(loadedTypes).Where(c => !c.IsGenericTypeDefinition).ToList();

            if (implementations.Count == 0)
                return Array.Empty<ServiceDescriptor>();

            return implementations.Count > 1
                ? contractType.RegisterAll(implementations, lifetimeFunc.Invoke(contractType))
                : contractType.Register(implementations.Single(), lifetimeFunc.Invoke(contractType));
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

        private static IEnumerable<ServiceDescriptor> Register(this Type service, Type implementation, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            //Console.WriteLine($"implementation: {implementation}; service: {service};");
            return new []
            {
                ServiceDescriptor.Describe(implementation, implementation, lifetime),
                ServiceDescriptor.Describe(service, provider => provider.GetRequiredService(implementation), lifetime)
            };
        }

        private static IEnumerable<ServiceDescriptor> RegisterAll(this Type service, IEnumerable<Type> implementationList, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            var result = new List<ServiceDescriptor>();

            foreach (var implementation in implementationList)
                result.AddRange(Register(service, implementation, lifetime));

            return result;
        }
    }
}
