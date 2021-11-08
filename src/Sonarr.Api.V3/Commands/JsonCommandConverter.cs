using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NzbDrone.Core.Messaging.Commands;

namespace Sonarr.Api.V3.Commands
{
    public class JsonCommandConverter : JsonConverter<Command>
    {
        public override Command Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, Command value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, (object) value, options);
    }
}
