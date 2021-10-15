using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NzbDrone.Common.Serializer
{
    public class JsonVersionConverter: JsonConverter<Version>
    {
        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Version.Parse(reader.GetString() ?? string.Empty);

        public override void Write(Utf8JsonWriter writer, Version versionValue, JsonSerializerOptions options) =>
            writer.WriteStringValue(versionValue.ToString());
    }
}
