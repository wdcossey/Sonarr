using System.Threading.Tasks;

namespace NzbDrone.Core.Messaging.Commands
{
    //TODO: Remove
    /*public interface IExecute<TCommand> : IProcessMessage<TCommand> where TCommand : Command
    {
        void Execute(TCommand message);
    }*/
    
    public interface IExecuteAsync<TCommand> : IProcessMessageAsync<TCommand> where TCommand : Command
    {
        Task ExecuteAsync(TCommand message);
    }
}