using System;

namespace NzbDrone.Core.Commands
{
    public interface ICommandFactory
    {

    }

    public class CommandFactory : ICommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CommandFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

    }
}
