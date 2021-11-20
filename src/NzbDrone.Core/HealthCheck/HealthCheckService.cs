using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Messaging;
using NzbDrone.Common.Reflection;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.HealthCheck
{
    public interface IHealthCheckService
    {
        List<HealthCheck> Results();
    }

    public class HealthCheckService : IHealthCheckService,
                                      IExecuteAsync<CheckHealthCommand>,
                                      IHandleAsync<ApplicationStartedEvent>,
                                      IHandleAsync<IEvent>
    {
        private readonly IProvideHealthCheck[] _healthChecks;
        private readonly IProvideHealthCheck[] _startupHealthChecks;
        private readonly IProvideHealthCheck[] _scheduledHealthChecks;
        private readonly Dictionary<Type, IEventDrivenHealthCheck[]> _eventDrivenHealthChecks;
        private readonly IEventAggregator _eventAggregator;
        private readonly ICacheManager _cacheManager;

        private readonly ICached<HealthCheck> _healthCheckResults;

        public HealthCheckService(IEnumerable<IProvideHealthCheck> healthChecks,
                                  IEventAggregator eventAggregator,
                                  ICacheManager cacheManager)
        {
            _healthChecks = healthChecks.ToArray();
            _eventAggregator = eventAggregator;
            _cacheManager = cacheManager;

            _healthCheckResults = _cacheManager.GetCache<HealthCheck>(GetType());

            _startupHealthChecks = _healthChecks.Where(v => v.CheckOnStartup).ToArray();
            _scheduledHealthChecks = _healthChecks.Where(v => v.CheckOnSchedule).ToArray();
            _eventDrivenHealthChecks = GetEventDrivenHealthChecks();
        }

        public List<HealthCheck> Results()
        {
            return _healthCheckResults.Values.ToList();
        }

        private Dictionary<Type, IEventDrivenHealthCheck[]> GetEventDrivenHealthChecks()
        {
            return _healthChecks
                .SelectMany(h => h.GetType().GetAttributes<CheckOnAttribute>().Select(a =>
                {
                    var eventDrivenType = typeof(EventDrivenHealthCheck<>).MakeGenericType(a.EventType);
                    var eventDriven = (IEventDrivenHealthCheck)Activator.CreateInstance(eventDrivenType, h, a.Condition);

                    return Tuple.Create(a.EventType, eventDriven);
                }))
                .GroupBy(t => t.Item1, t => t.Item2)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        private void PerformHealthCheck(IProvideHealthCheck[] healthChecks)
        {
            var results = healthChecks.Select(c => c.Check())
                                       .ToList();

            foreach (var result in results)
            {
                if (result.Type == HealthCheckResult.Ok)
                {
                    _healthCheckResults.Remove(result.Source.Name);
                }

                else
                {
                    if (_healthCheckResults.Find(result.Source.Name) == null)
                    {
                        _eventAggregator.PublishEvent(new HealthCheckFailedEvent(result));
                    }

                    _healthCheckResults.Set(result.Source.Name, result);
                }
            }

            _eventAggregator.PublishEvent(new HealthCheckCompleteEvent());
        }

        public Task ExecuteAsync(CheckHealthCommand message)
        {
            PerformHealthCheck(message.Trigger == CommandTrigger.Manual 
                ? _healthChecks 
                : _scheduledHealthChecks);
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(ApplicationStartedEvent message)
        {
            PerformHealthCheck(_startupHealthChecks);
            return Task.CompletedTask;
        }

        public Task HandleAsync(IEvent message)
        {
            if (message is HealthCheckCompleteEvent)
                return Task.CompletedTask;;

            if (!_eventDrivenHealthChecks.TryGetValue(message.GetType(), out var checks))
                return Task.CompletedTask;;

            var filteredChecks = new List<IProvideHealthCheck>();
            var healthCheckResults = _healthCheckResults.Values.ToList();

            foreach (var eventDrivenHealthCheck in checks)
            {
                var healthCheckType = eventDrivenHealthCheck.HealthCheck.GetType();
                var previouslyFailed = healthCheckResults.Any(r => r.Source == healthCheckType);

                if (!eventDrivenHealthCheck.ShouldExecute(message, previouslyFailed)) 
                    continue;
                
                filteredChecks.Add(eventDrivenHealthCheck.HealthCheck);
            }

            // TODO: Add debounce

            PerformHealthCheck(filteredChecks.ToArray());
            
            return Task.CompletedTask;
        }
    }
}
