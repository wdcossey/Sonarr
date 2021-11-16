using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Nancy;
using Nancy.Responses;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Serializer;

namespace Sonarr.Http.Extensions
{
    public static class ReqResExtensions
    {
        private static readonly NancyJsonSerializer NancySerializer = new NancyJsonSerializer();
        private static readonly string Expires = DateTime.UtcNow.AddYears(1).ToString("r");

        public static readonly string LastModified = BuildInfo.BuildDateTime.ToString("r");

        public static T FromJson<T>(this Stream body) where T : class, new()
            => FromJson<T>(body, typeof(T));

        public static Task<T> FromJsonAsync<T>(this Stream body) where T : class, new()
            => FromJsonAsync<T>(body, typeof(T));

        public static T FromJson<T>(this Stream body, Type type)
            => (T)FromJson(body, type);

        public static Task<T> FromJsonAsync<T>(this Stream body, Type type)
            => FromJsonAsync(body, type) as Task<T>;

        public static object FromJson(this Stream body, Type type)
        {
            using var reader = new StreamReader(body, true);
            body.Seek(0, SeekOrigin.Begin);
            var value = reader.ReadToEnd();
            return Json.Deserialize(value, type);
        }

        public static Task<object> FromJsonAsync(this Stream body, Type type)
            => Json.DeserializeAsync(body, type);

        public static JsonResponse<TModel> AsResponse<TModel>(this TModel model, NancyContext context, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = new JsonResponse<TModel>(model, NancySerializer, context.Environment) { StatusCode = statusCode };
            response.Headers.DisableCache();

            return response;
        }

        public static IDictionary<string, string> DisableCache(this IDictionary<string, string> headers)
        {
            headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";

            return headers;
        }

        public static IDictionary<string, string> EnableCache(this IDictionary<string, string> headers)
        {
            headers["Cache-Control"] = "max-age=31536000, public";
            headers["Expires"] = Expires;
            headers["Last-Modified"] = LastModified;
            headers["Age"] = "193266";

            return headers;
        }
    }
}
