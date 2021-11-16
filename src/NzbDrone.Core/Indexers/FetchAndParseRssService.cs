using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Common.TPL;
using System;
namespace NzbDrone.Core.Indexers
{
    public interface IFetchAndParseRss
    {
        List<ReleaseInfo> Fetch();
    }

    public class FetchAndParseRssService : IFetchAndParseRss
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly ILogger<FetchAndParseRssService> _logger;

        public FetchAndParseRssService(IIndexerFactory indexerFactory, ILogger<FetchAndParseRssService> logger)
        {
            _indexerFactory = indexerFactory;
            _logger = logger;
        }

        public List<ReleaseInfo> Fetch()
        {
            var result = new List<ReleaseInfo>();

            var indexers = _indexerFactory.RssEnabled();

            if (!indexers.Any())
            {
                _logger.LogWarning("No available indexers. check your configuration.");
                return result;
            }

            _logger.LogDebug("Available indexers {Count}", indexers.Count);

            var taskList = new List<Task>();
            var taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

            foreach (var indexer in indexers)
            {
                var indexerLocal = indexer;

                var task = taskFactory.StartNew(() =>
                     {
                         try
                         {
                             var indexerReports = indexerLocal.FetchRecent();

                             lock (result)
                             {
                                 _logger.LogDebug("Found {Count} from {Name}", indexerReports.Count, indexer.Name);

                                 result.AddRange(indexerReports);
                             }
                         }
                         catch (Exception e)
                         {
                             _logger.LogError(e, "Error during RSS Sync");
                         }
                     }).LogExceptions();

                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());

            _logger.LogDebug("Found {0} reports", result.Count);

            return result;
        }
    }
}
