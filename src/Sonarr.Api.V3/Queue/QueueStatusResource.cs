using Sonarr.Http.Attributes;
using Sonarr.Http.REST;

namespace Sonarr.Api.V3.Queue
{
    [BroadcastName("QueueStatus")]
    public class QueueStatusResource : RestResource
    {
        public int TotalCount { get; set; }
        public int Count { get; set; }
        public int UnknownCount { get; set; }
        public bool Errors { get; set; }
        public bool Warnings { get; set; }
        public bool UnknownErrors { get; set; }
        public bool UnknownWarnings { get; set; }
    }
}
