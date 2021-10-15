using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NzbDrone.Common.Http;

namespace NzbDrone.Common.Serializer
{
    public class JsonHttpUriConverter: JsonConverter<HttpUri>
    {
        public override HttpUri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString());

        public override void Write(Utf8JsonWriter writer, HttpUri httpUriValue, JsonSerializerOptions options)
        {
            if (httpUriValue == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue((httpUriValue).FullUri);
        }
    }
}