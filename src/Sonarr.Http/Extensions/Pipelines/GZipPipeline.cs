using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Nancy;
using Nancy.Bootstrapper;
using Microsoft.Extensions.Logging;

namespace Sonarr.Http.Extensions.Pipelines
{
    public class GzipCompressionPipeline : IRegisterNancyPipeline
    {
        private readonly ILogger<GzipCompressionPipeline> _logger;

        public int Order => 0;

        private readonly Action<Action<Stream>, Stream> _writeGZipStream;

        public GzipCompressionPipeline(ILogger<GzipCompressionPipeline> logger)
        {
            _logger = logger;
            _writeGZipStream = WriteGZipStream;
        }

        public void Register(IPipelines pipelines)
        {
            pipelines.AfterRequest.AddItemToEndOfPipeline(CompressResponse);
        }

        private void CompressResponse(NancyContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                if (
                   response.Contents != Response.NoBody
                && !response.ContentType.Contains("image")
                && !response.ContentType.Contains("font")
                && request.Headers.AcceptEncoding.Any(x => x.Contains("gzip"))
                && !AlreadyGzipEncoded(response)
                && !ContentLengthIsTooSmall(response))
                {
                    var contents = response.Contents;

                    response.Headers["Content-Encoding"] = "gzip";
                    response.Contents = responseStream => _writeGZipStream(contents, responseStream);
                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to gzip response");
                throw;
            }
        }

        private static void WriteGZipStreamMono(Action<Stream> innerContent, Stream targetStream)
        {
            using (var membuffer = new MemoryStream())
            {
                WriteGZipStream(innerContent, membuffer);
                membuffer.Position = 0;
                membuffer.CopyTo(targetStream);
            }
        }

        private static void WriteGZipStream(Action<Stream> innerContent, Stream targetStream)
        {
            using (var gzip = new GZipStream(targetStream, CompressionMode.Compress, true))
            using (var buffered = new BufferedStream(gzip, 8192))
            {
                innerContent.Invoke(buffered);
            }
        }

        private static bool ContentLengthIsTooSmall(Response response)
        {
            if (response.Headers.TryGetValue("Content-Length", out var contentLength)
                && !string.IsNullOrWhiteSpace(contentLength) && long.Parse(contentLength) < 1024)
            {
                return true;
            }

            return false;
        }

        private static bool AlreadyGzipEncoded(Response response)
        {
            if (response.Headers.TryGetValue("Content-Length", out var contentEncoding)
                && !string.IsNullOrWhiteSpace(contentEncoding) && contentEncoding == "gzip" )
            {
                return true;
            }

            return false;
        }
    }
}
