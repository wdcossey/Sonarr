using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Messaging;
using NzbDrone.Common.TPL;
using IServiceProvider = System.IServiceProvider;

namespace NzbDrone.Core.Messaging.Events
{
    public class EventAggregator : IEventAggregator
    {
        private readonly ILogger<EventAggregator> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TaskFactory _taskFactory;
        private readonly Dictionary<string, object> _eventSubscribers;

        private class EventSubscribers<TEvent> where TEvent : class, IEvent
        {
            public IHandle<TEvent>[] _syncHandlers;
            public IHandleAsync<TEvent>[] _asyncHandlers;
            public IHandleAsync<IEvent>[] _globalHandlers;

            public EventSubscribers(IServiceProvider serviceProvider)
            {
                _syncHandlers = serviceProvider.GetServices<IHandle<TEvent>>()
                                              .OrderBy(GetEventHandleOrder)
                                              .ToArray();

                _globalHandlers = serviceProvider.GetServices<IHandleAsync<IEvent>>()
                                              .ToArray();

                _asyncHandlers = serviceProvider.GetServices<IHandleAsync<TEvent>>()
                                               .ToArray();
            }
        }

        public EventAggregator(ILogger<EventAggregator> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _taskFactory = new TaskFactory();
            _eventSubscribers = new Dictionary<string, object>();
        }

        public void PublishEvent<TEvent>(TEvent @event) where TEvent : class, IEvent
        {
            Ensure.That(@event, () => @event).IsNotNull();

            var eventName = GetEventName(@event.GetType());

            /*
                        int workerThreads;
                        int completionPortThreads;
                        ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);

                        int maxCompletionPortThreads;
                        int maxWorkerThreads;
                        ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);


                        int minCompletionPortThreads;
                        int minWorkerThreads;
                        ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionPortThreads);

                        _logger.Warn("Thread pool state WT:{0} PT:{1}  MAXWT:{2} MAXPT:{3} MINWT:{4} MINPT:{5}", workerThreads, completionPortThreads, maxWorkerThreads, maxCompletionPortThreads, minWorkerThreads, minCompletionPortThreads);
            */

            _logger.LogTrace("Publishing {EventName}", eventName);

            EventSubscribers<TEvent> subscribers;
            lock (_eventSubscribers)
            {
                if (!_eventSubscribers.TryGetValue(eventName, out var target))
                {
                    _eventSubscribers[eventName] = target = new EventSubscribers<TEvent>(_serviceProvider);
                }

                subscribers = target as EventSubscribers<TEvent>;
            }

            //call synchronous handlers first.
            var handlers = subscribers._syncHandlers;
            foreach (var handler in handlers)
            {
                try
                {
                    _logger.LogTrace("{EventName} -> {TypeName}", eventName, handler.GetType().Name);
                    handler.Handle(@event);
                    _logger.LogTrace("{EventName} <- {TypeName}", eventName, handler.GetType().Name);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "{TypeName} failed while processing [{EventName}]", handler.GetType().Name, eventName);
                }
            }

            foreach (var handler in subscribers._globalHandlers)
            {
                var handlerLocal = handler;

                _taskFactory.StartNew(() =>
                {
                    handlerLocal.HandleAsync(@event);
                }, TaskCreationOptions.PreferFairness)
                .LogExceptions();
            }

            foreach (var handler in subscribers._asyncHandlers)
            {
                var handlerLocal = handler;

                _taskFactory.StartNew(() =>
                {
                    _logger.LogTrace("{EventName} ~> {TypeName}", eventName, handlerLocal.GetType().Name);
                    handlerLocal.HandleAsync(@event);
                    _logger.LogTrace("{EventName} <~ {TypeName}", eventName, handlerLocal.GetType().Name);
                }, TaskCreationOptions.PreferFairness)
                .LogExceptions();
            }
        }

        private static string GetEventName(Type eventType)
        {
            if (!eventType.IsGenericType)
            {
                return eventType.Name;
            }

            return $"{eventType.Name.Remove(eventType.Name.IndexOf('`'))}<{eventType.GetGenericArguments()[0].Name}>";
        }

        internal static int GetEventHandleOrder<TEvent>(IHandle<TEvent> eventHandler) where TEvent : class, IEvent
        {
            var method = eventHandler.GetType().GetMethod(nameof(eventHandler.Handle), new Type[] {typeof(TEvent)});

            if (method == null)
            {
                return (int) EventHandleOrder.Any;
            }

            if (method.GetCustomAttributes(typeof(EventHandleOrderAttribute), true).FirstOrDefault() is not EventHandleOrderAttribute attribute)
            {
                return (int) EventHandleOrder.Any;
            }

            return (int)attribute.EventHandleOrder;
        }
    }
}
