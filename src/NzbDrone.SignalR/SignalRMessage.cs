using Newtonsoft.Json;
using NzbDrone.Core.Datastore.Events;

namespace NzbDrone.SignalR
{

    public class SignalRMessage<TBody> where TBody: class
    {
        public TBody Body { get; set; }

        public string Name { get; set; }

        [JsonIgnore]
        public ModelAction Action { get; set; }
    }

    public class SignalRMessage : SignalRMessage<object> { }


}