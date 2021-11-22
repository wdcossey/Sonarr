using System;
using System.Threading.Tasks;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Messaging.Events
{
    public interface IHandleAsync<TEvent> : IProcessMessageAsync<TEvent> where TEvent : IEvent
    {
        Task HandleAsync(TEvent message);
    }
}