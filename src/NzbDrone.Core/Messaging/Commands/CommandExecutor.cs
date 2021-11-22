using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Extensions;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ProgressMessaging;

namespace NzbDrone.Core.Messaging.Commands
{
    public class CommandExecutor : IHandleAsync<ApplicationStartedEvent>,
                                   IHandleAsync<ApplicationShutdownRequested>
    {
        private readonly ILogger<CommandExecutor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IEventAggregator _eventAggregator;

        private static CancellationTokenSource _cancellationTokenSource;
        private const int THREAD_LIMIT = 3;

        public CommandExecutor(IServiceProvider serviceProvider,
                               IManageCommandQueue commandQueueManager,
                               IEventAggregator eventAggregator,
                               ILogger<CommandExecutor> logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _commandQueueManager = commandQueueManager;
            _eventAggregator = eventAggregator;
        }

        private async Task ExecuteCommandsAsync()
        {
            try
            {
                foreach (var command in _commandQueueManager.Queue(_cancellationTokenSource.Token))
                {
                    try
                    {
                        await ExecuteCommandAsync((dynamic) command.Body, command);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred while executing task {CommandName}", command.Name);
                    }
                }
            }
            catch (ThreadAbortException ex)
            {
                _logger.LogError(ex, "Thread aborted");
                Thread.ResetAbort();
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("Stopped one command execution pipeline");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error in thread");
            }
        }

        private async Task ExecuteCommandAsync<TCommand>(TCommand command, CommandModel commandModel) where TCommand : Command
        {
            IExecuteAsync<TCommand> handler = null;

            try
            {
                handler = _serviceProvider.GetRequiredService<IExecuteAsync<TCommand>>();
                
                _logger.LogTrace("{CommandTypeName} -> {HandlerTypeName}", command.GetType().Name, handler.GetType().Name);

                _commandQueueManager.Start(commandModel);
                BroadcastCommandUpdate(commandModel);

                ProgressMessageContext.CommandModel ??= commandModel;

                await handler.ExecuteAsync(command);

                _commandQueueManager.Complete(commandModel, command.CompletionMessage ?? commandModel.Message);
            }
            catch (CommandFailedException ex)
            {
                _commandQueueManager.SetMessage(commandModel, "Failed");
                _commandQueueManager.Fail(commandModel, ex.Message, ex);
                throw;
            }
            catch (Exception ex)
            {
                _commandQueueManager.SetMessage(commandModel, "Failed");
                _commandQueueManager.Fail(commandModel, "Failed", ex);
                throw;
            }
            finally
            {
                BroadcastCommandUpdate(commandModel);

                _eventAggregator.PublishEvent(new CommandExecutedEvent(commandModel));

                if (ProgressMessageContext.CommandModel == commandModel)
                {
                    ProgressMessageContext.CommandModel = null;
                }

                if (handler != null)
                {
                    _logger.LogTrace("{CommandTypeName} <- {HandlerTypeName} [{Duration}]", command.GetType().Name, handler.GetType().Name, commandModel.Duration.ToString());
                }
            }
        }

        private void BroadcastCommandUpdate(CommandModel command)
        {
            if (command.Body.SendUpdatesToClient)
            {
                _eventAggregator.PublishEvent(new CommandUpdatedEvent(command));
            }
        }

        public Task HandleAsync(ApplicationStartedEvent message)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            for (int i = 0; i < Math.Max(THREAD_LIMIT, Environment.ProcessorCount) ; i++)
            {
                Task.Run(ExecuteCommandsAsync).Forget();
                //var thread = new Thread(() => ExecuteCommandsAsync());
                //thread.Start();
            }
            
            return Task.CompletedTask;
            /*for (int i = 0; i < THREAD_LIMIT; i++)
            {
                var thread = new Thread(ExecuteCommands);
                thread.Start();
            }*/
        }

        public Task HandleAsync(ApplicationShutdownRequested message)
        {
            _logger.LogInformation("Shutting down task execution");
            _cancellationTokenSource.Cancel(true);
            return Task.CompletedTask;
        }
    }
}
