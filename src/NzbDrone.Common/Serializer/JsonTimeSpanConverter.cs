using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NzbDrone.Common.Serializer
{
    public class JsonTimeSpanConverter: JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TimeSpan.Parse(reader.GetString() ?? "00:00:00.0");

        public override void Write(Utf8JsonWriter writer, TimeSpan timeSpanValue, JsonSerializerOptions options) 
            => writer.WriteStringValue(timeSpanValue.ToString("c"));
    }
    
    public class JsonTimeSpanNullableConverter: JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var timeSpanStr = reader.GetString();
            return !string.IsNullOrWhiteSpace(timeSpanStr) ? TimeSpan.Parse(timeSpanStr) : null;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? timeSpanValue, JsonSerializerOptions options) 
            => writer.WriteStringValue(timeSpanValue?.ToString("c"));
    }
}
