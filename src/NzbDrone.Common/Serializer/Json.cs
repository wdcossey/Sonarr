using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;


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
                IgnoreNullValues = true,
                //DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                WriteIndented = true
                /*DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                DefaultValueHandling = DefaultValueHandling.Include,
                ContractResolver = new CamelCasePropertyNamesContractResolver()*/
            };

            serializerSettings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            serializerSettings.Converters.Add(new JsonVersionConverter());
            serializerSettings.Converters.Add(new JsonHttpUriConverter());

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

        private static JsonException DetailedJsonReaderException(JsonException ex, string json)
        {
            var lineNumber = ex.LineNumber == 0 ? 0 : (ex.LineNumber - 1) ?? 0;
            var linePosition = ex.BytePositionInLine ?? 0;

            var lines = json.Split('\n');
            if (lineNumber >= 0 && lineNumber < lines.Length &&
                linePosition >= 0 && linePosition < lines[lineNumber].Length)
            {
                var line = lines[lineNumber];
                var start = Math.Max(0, linePosition - 20);
                var end = Math.Min(line.Length, linePosition + 20);

                var snippetBefore = line.Substring((int)start, (int)(linePosition - start));
                var snippetAfter = line.Substring((int)linePosition, (int)(end - linePosition));
                var message = ex.Message + " (Json snippet '" + snippetBefore + "<--error-->" + snippetAfter + "')";

                // Not risking updating JSON.net from 9.x to 10.x just to get this as public ctor.
                var ctor = typeof(JsonException).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string), typeof(Exception), typeof(string), typeof(int), typeof(int) }, null);
                if (ctor != null)
                {
                    return (JsonException)ctor.Invoke(new object[] { message, ex, ex.Path, ex.LineNumber, linePosition });
                }

                // JSON.net 10.x ctor in case we update later.
                ctor = typeof(JsonException).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(string), typeof(string), typeof(int), typeof(int), typeof(Exception) }, null);
                if (ctor != null)
                {
                    return (JsonException)ctor.Invoke(new object[] { message, ex.Path, ex.LineNumber, linePosition, ex });
                }
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
