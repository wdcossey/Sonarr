using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace NzbDrone.Core.Messaging.Commands
{
    public interface ICommandFactory
    {
        Command Create(string name);

        Command Create(Type type);

        Type GetCommandType(string name);
    }

    public class CommandFactory : ICommandFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<Type> _validCommandTypes;

        public CommandFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _validCommandTypes = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(c => c.FullName.Contains("Sonarr")) //Only interested in `Sonarr` assemblies!
                .SelectMany(assembly=> assembly.GetExportedTypes())
                .Where(type => !type.IsInterface && !type.IsAbstract && typeof(Command).IsAssignableFrom(type))
                .Distinct();
        }

        public Command Create(string name)
        {
            var commandType = GetCommandType(name);
            return Create(commandType);
        }

        public Command Create(Type commandType)
            => (Command)ActivatorUtilities.CreateInstance(_serviceProvider, commandType);

        public Type GetCommandType(string name)
        {
            return
                _validCommandTypes
                    .Single(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) || c.Name.Replace(nameof(Command), string.Empty).Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
