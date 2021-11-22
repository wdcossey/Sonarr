using System;

namespace NzbDrone.Core.Messaging
{
    public interface IProcessMessage { }

    public interface IProcessMessageAsync : IProcessMessage { }
    
    public interface IProcessMessageAsync<TMessage> : IProcessMessageAsync { }
}