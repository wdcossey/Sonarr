using System;
using Sonarr.Http.ClientSchema;

namespace Sonarr.Blazor.Shared.ClientSchema
{
    public class FieldMapping
    {
        public Field Field { get; set; }
        public Type PropertyType { get; set; }
        public Func<object, object> GetterFunc { get; set; }
        public Action<object, object> SetterFunc { get; set; }
    }
}
