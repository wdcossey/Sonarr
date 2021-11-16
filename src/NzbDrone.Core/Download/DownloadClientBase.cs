using System;
using System.Collections.Generic;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download
{
    public abstract class DownloadClientBase<TSettings> : IDownloadClient
        where TSettings : IProviderConfig, new()
    {
        protected readonly IConfigService _configService;
        protected readonly IDiskProvider _diskProvider;
        protected readonly IRemotePathMappingService _remotePathMappingService;
        protected readonly ILogger _logger;

        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }

        public virtual object RequestAction(string action, IDictionary<string, string> query) { return null; }

        protected TSettings Settings => (TSettings)Definition.Settings;

        protected DownloadClientBase(IConfigService configService,
            IDiskProvider diskProvider,
            IRemotePathMappingService remotePathMappingService,
            ILogger logger)
        {
            _configService = configService;
            _diskProvider = diskProvider;
            _remotePathMappingService = remotePathMappingService;
            _logger = logger;
        }

        public override string ToString()
        {
            return GetType().Name;
        }

        public abstract DownloadProtocol Protocol
        {
            get;
        }

        public abstract string Download(RemoteEpisode remoteEpisode);
        public abstract IEnumerable<DownloadClientItem> GetItems();

        public virtual DownloadClientItem GetImportItem(DownloadClientItem item, DownloadClientItem previousImportAttempt)
        {
            return item;
        }

        public abstract void RemoveItem(DownloadClientItem item, bool deleteData);
        public abstract DownloadClientInfo GetStatus();

        protected virtual void DeleteItemData(DownloadClientItem item)
        {
            if (item == null)
            {
                return;
            }

            if (item.OutputPath.IsEmpty)
            {
                _logger.LogTrace("[{Title}] Doesn't have an outputPath, skipping delete data.", item.Title);
                return;
            }

            try
            {
                if (_diskProvider.FolderExists(item.OutputPath.FullPath))
                {
                    _logger.LogDebug("[{Title}] Deleting folder '{OutputPath}'.", item.Title, item.OutputPath);

                    _diskProvider.DeleteFolder(item.OutputPath.FullPath, true);
                }
                else if (_diskProvider.FileExists(item.OutputPath.FullPath))
                {
                    _logger.LogDebug("[{Title}] Deleting file '{OutputPath}'.", item.Title, item.OutputPath);

                    _diskProvider.DeleteFile(item.OutputPath.FullPath);
                }
                else
                {
                    _logger.LogTrace("[{Title}] File or folder '{OutputPath}' doesn't exist, skipping cleanup.", item.Title, item.OutputPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{Title}] Error occurred while trying to delete data from '{OutputPath}'.", item.Title, item.OutputPath);
            }
        }

        public ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                Test(failures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test aborted due to exception");
                failures.Add(new ValidationFailure(string.Empty, "Test was aborted due to an error: " + ex.Message));
            }

            return new ValidationResult(failures);
        }

        protected abstract void Test(List<ValidationFailure> failures);

        protected ValidationFailure TestFolder(string folder, string propertyName, bool mustBeWritable = true)
        {
            if (!_diskProvider.FolderExists(folder))
            {
                return new NzbDroneValidationFailure(propertyName, "Folder does not exist")
                {
                    DetailedDescription = string.Format("The folder you specified does not exist or is inaccessible. Please verify the folder permissions for the user account '{0}', which is used to execute Sonarr.", Environment.UserName)
                };
            }

            if (mustBeWritable && !_diskProvider.FolderWritable(folder))
            {
                _logger.LogError("Folder '{Folder}' is not writable.", folder);
                return new NzbDroneValidationFailure(propertyName, "Unable to write to folder")
                {
                    DetailedDescription = string.Format("The folder you specified is not writable. Please verify the folder permissions for the user account '{0}', which is used to execute Sonarr.", Environment.UserName)
                };
            }

            return null;
        }

        public virtual void MarkItemAsImported(DownloadClientItem downloadClientItem)
        {
            throw new NotSupportedException(this.Name + " does not support marking items as imported");
        }
    }
}
