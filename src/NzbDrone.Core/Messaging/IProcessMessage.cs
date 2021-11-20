using System;

namespace NzbDrone.Core.Messaging
{
    public interface IProcessMessage { }

    public interface IProcessMessageAsync : IProcessMessage { }

    //TODO: Remove
    [Obsolete("Use IProcessMessageAsync<>()", true)]
    public interface IProcessMessage<TMessage> : IProcessMessage { }

    public interface IProcessMessageAsync<TMessage> : IProcessMessageAsync { }
}