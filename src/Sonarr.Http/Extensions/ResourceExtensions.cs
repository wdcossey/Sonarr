using System;
using System.Reflection;
using NzbDrone.Common.Extensions;
using Sonarr.Http.Attributes;
using Sonarr.Http.REST;

namespace NzbDrone.Http.Extensions
{
    internal static class ResourceExtensions
    {
        public static string GetBroadcastName<TResource>(this TResource resource)
            where TResource : RestResource => resource.GetType().GetBroadcastName();

        public static string GetBroadcastName(this Type type)
        {
            var attribute = type.GetCustomAttribute<BroadcastNameAttribute>();
            
            if (attribute != null && attribute.Name.IsNotNullOrWhiteSpace())
                return attribute.Name;

            return typeof(RestResource).IsAssignableFrom(type) ? type.Name.Replace("Resource", string.Empty, StringComparison.InvariantCultureIgnoreCase) : null;
        }
    }
}
