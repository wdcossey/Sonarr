using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NzbDrone.Common.Serializer
{
    public static class Json
    {
        private static readonly JsonSerializerOptions SerializerOptions;

        static Json()
        {
            SerializerOptions = GetSerializerOptions();
        }

        public static JsonSerializerOptions GetSerializerOptions()
        {
            var serializerSettings = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            };

            serializerSettings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            serializerSettings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            serializerSettings.Converters.Add(new JsonVersionConverter());
            serializerSettings.Converters.Add(new JsonHttpUriConverter());
            serializerSettings.Converters.Add(new JsonTimeSpanConverter());
            serializerSettings.Converters.Add(new JsonTimeSpanNullableConverter());
            serializerSettings.Converters.Add(new JsonBigIntegerConverter());

            return serializerSettings;
        }

        public static T Deserialize<T>(string json) where T : new()
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, SerializerOptions);
            }
            catch (JsonException ex)
            {
                throw DetailedJsonReaderException(ex, json);
            }
        }

        public static object Deserialize(string json, Type type)
        {
            try
            {
                return JsonSerializer.Deserialize(json, type, SerializerOptions);
            }
            catch (JsonException ex)
            {
                throw DetailedJsonReaderException(ex, json);
            }
        }

        public static async Task<object> DeserializeAsync(Stream utf8Json, Type type)
        {
            try
            {
                utf8Json.Seek(0, SeekOrigin.Begin);
                return await JsonSerializer.DeserializeAsync(utf8Json, type, SerializerOptions);
            }
            catch (JsonException ex)
            {
                using var reader = new StreamReader(utf8Json, Encoding.UTF8);
                throw DetailedJsonReaderException(ex, await reader.ReadToEndAsync());
            }
        }

        public static async Task<T> DeserializeAsync<T>(Stream utf8Json) where T : new()
        {
            try
            {
                utf8Json.Seek(0, SeekOrigin.Begin);
                return await JsonSerializer.DeserializeAsync<T>(utf8Json, SerializerOptions);
            }
            catch (JsonException ex)
            {
                using var reader = new StreamReader(utf8Json, Encoding.UTF8);
                throw DetailedJsonReaderException(ex, await reader.ReadToEndAsync());
            }
        }

        private static JsonException DetailedJsonReaderException(JsonException ex, string json)
        {
            var lineNumber = Convert.ToInt32(ex.LineNumber ?? 0);
            var linePosition = Convert.ToInt32(ex.BytePositionInLine ?? 0);

            var lines = json.Split('\n');
            if (lineNumber >= 0 && lineNumber < lines.Length)
            {
                var line = lines[lineNumber];
                var start = Math.Max(0, linePosition - 20);
                var end = Math.Min(line.Length, linePosition + 20);

                var snippetBefore = line.Substring(start, linePosition - start);
                var snippetAfter = line.Substring(linePosition, end - linePosition);
                var message = ex.Message + " (Json snippet '" + snippetBefore + "<--error-->" + snippetAfter + "')";

                return new JsonException(message, ex.Path, ex.LineNumber, linePosition, ex.InnerException);
            }

            return ex;
        }

        public static bool TryDeserialize<T>(string json, out T result) where T : new()
        {
            try
            {
                result = Deserialize<T>(json);
                return true;
            }
            catch (JsonException)
            {
                result = default(T);
                return false;
            }
        }

        public static string ToJson(this object obj)
        {
            return JsonSerializer.Serialize(obj, SerializerOptions);
        }

        /*public static void Serialize<TModel>(TModel model, TextWriter outputStream)
        {
            var jsonTextWriter = new Utf8JsonWriter(outputStream);
            JsonSerializer.Serialize(jsonTextWriter, model);
            jsonTextWriter.Flush();
        }*/

        public static void Serialize<TModel>(TModel model, Stream outputStream)
        {
            var jsonTextWriter = new Utf8JsonWriter(outputStream);
            JsonSerializer.Serialize(jsonTextWriter, model);
            jsonTextWriter.Flush();
        }
    }
}
