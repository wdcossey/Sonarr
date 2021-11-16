using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Common.TPL;
using System;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.ImportLists
{
    public interface IFetchAndParseImportList
    {
        List<ImportListItemInfo> Fetch();
        List<ImportListItemInfo> FetchSingleList(ImportListDefinition definition);
    }

    public class FetchAndParseImportListService : IFetchAndParseImportList
    {
        private readonly IImportListFactory _importListFactory;
        private readonly ILogger<FetchAndParseImportListService> _logger;

        public FetchAndParseImportListService(IImportListFactory importListFactory, ILogger<FetchAndParseImportListService> logger)
        {
            _importListFactory = importListFactory;
            _logger = logger;
        }

        public List<ImportListItemInfo> Fetch()
        {
            var result = new List<ImportListItemInfo>();

            var importLists = _importListFactory.AutomaticAddEnabled();

            if (!importLists.Any())
            {
                _logger.LogDebug("No enabled import lists, skipping.");
                return result;
            }

            _logger.LogDebug("Available import lists {Count}", importLists.Count);

            var taskList = new List<Task>();
            var taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

            foreach (var importList in importLists)
            {
                var importListLocal = importList;

                var task = taskFactory.StartNew(() =>
                     {
                         try
                         {
                             var importListReports = importListLocal.Fetch();

                             lock (result)
                             {
                                 _logger.LogDebug("Found {Count} from {Name}", importListReports.Count, importList.Name);

                                 result.AddRange(importListReports);
                             }
                         }
                         catch (Exception e)
                         {
                             _logger.LogError(e, "Error during Import List Sync");
                         }
                     }).LogExceptions();

                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());

            result = result.DistinctBy(r => new {r.TvdbId, r.Title}).ToList();

            _logger.LogDebug("Found {Count} reports", result.Count);

            return result;
        }

        public List<ImportListItemInfo> FetchSingleList(ImportListDefinition definition)
        {
            var result = new List<ImportListItemInfo>();

            var importList = _importListFactory.GetInstance(definition);

            if (importList == null || !definition.EnableAutomaticAdd)
            {
                _logger.LogDebug("Import list not enabled, skipping.");
                return result;
            }

            var taskList = new List<Task>();
            var taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

            var importListLocal = importList;

            var task = taskFactory.StartNew(() =>
            {
                try
                {
                    var importListReports = importListLocal.Fetch();

                    lock (result)
                    {
                        _logger.LogDebug("Found {Count} from {Name}", importListReports.Count, importList.Name);

                        result.AddRange(importListReports);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error during Import List Sync");
                }
            }).LogExceptions();

            taskList.Add(task);


            Task.WaitAll(taskList.ToArray());

            result = result.DistinctBy(r => new { r.TvdbId, r.Title }).ToList();

            return result;
        }
    }
}
