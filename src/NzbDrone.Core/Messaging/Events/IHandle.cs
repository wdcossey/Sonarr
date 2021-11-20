using System;
using System.Threading.Tasks;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Messaging.Events
{
    //TODO: Remove
    [Obsolete("Use IProcessMessageAsync<>()", true)]
    public interface IHandle<TEvent> : IProcessMessage<TEvent> where TEvent : IEvent
    {
        void Handle(TEvent message);
    }

    public interface IHandleAsync<TEvent> : IProcessMessageAsync<TEvent> where TEvent : IEvent
    {
        Task HandleAsync(TEvent message);
    }
}