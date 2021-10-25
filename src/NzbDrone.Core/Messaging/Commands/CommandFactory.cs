using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Messaging.Commands
{
    public interface ICommandFactory
    {
        Command Create(string name);

        Command Create(string name, string body);

        Task<Command> CreateAsync(string name, Stream utf8Json);

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

        public Command Create(string name, string body)
        {
            var commandType = GetCommandType(name);
            return (Command)Json.Deserialize(body, commandType);
        }

        public async Task<Command> CreateAsync(string name, Stream utf8Json)
        {
            var commandType = GetCommandType(name);
            return (Command)(await Json.DeserializeAsync(utf8Json, commandType));
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
