using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Sonarr.Blazor.Server
{
    public class SonarrLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public SonarrLoadContext()
            : base(false)
        {
            _resolver = new AssemblyDependencyResolver(AppDomain.CurrentDomain.BaseDirectory);
            //_resolver.ResolveAssemblyToPath()
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

            return LoadFromAssemblyPath(assemblyPath);


            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
