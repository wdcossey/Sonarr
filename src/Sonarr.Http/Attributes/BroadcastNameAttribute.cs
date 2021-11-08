using System;

namespace Sonarr.Http.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class BroadcastNameAttribute : Attribute
    {
        public string Name { get; }

        public BroadcastNameAttribute(string name)
            => Name = name;
    }
}
