using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using FluentValidation;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.SignalR;
using Sonarr.Api.V3.RootFolders;

namespace NzbDrone.Host.WebHost.RootFolders
{
    public class RootFolderModule: WebApiController
    {
        private readonly IRootFolderService _rootFolderService;

        public RootFolderModule(IRootFolderService rootFolderService,
                                IBroadcastSignalRMessage signalRBroadcaster,
                                RootFolderValidator rootFolderValidator,
                                PathExistsValidator pathExistsValidator,
                                MappedNetworkDriveValidator mappedNetworkDriveValidator,
                                StartupFolderValidator startupFolderValidator,
                                SystemFolderValidator systemFolderValidator,
                                FolderWritableValidator folderWritableValidator
            )
            //: base(signalRBroadcaster)
        {
            _rootFolderService = rootFolderService;

            //GetResourceAll = GetRootFolders;
            //GetResourceById = GetRootFolder;
            //CreateResource = CreateRootFolder;
            //DeleteResource = DeleteFolder;

            /*SharedValidator.RuleFor(c => c.Path)
                           .Cascade(CascadeMode.StopOnFirstFailure)
                           .IsValidPath()
                           .SetValidator(rootFolderValidator)
                           .SetValidator(mappedNetworkDriveValidator)
                           .SetValidator(startupFolderValidator)
                           .SetValidator(pathExistsValidator)
                           .SetValidator(systemFolderValidator)
                           .SetValidator(folderWritableValidator);*/
        }

        [Route(HttpVerbs.Get, "/{id}")]
        public Task<RootFolderResource> GetRootFolderAsync(int id)
        {
            var result = _rootFolderService.Get(id, true).ToResource();
            return Task.FromResult<RootFolderResource>(result);
        }

        [Route(HttpVerbs.Post, "/")]
        public async Task<int> CreateRootFolderAsync()
        {
            var bodyContent = await HttpContext.GetRequestBodyAsStringAsync();
            var rootFolderResource = JsonSerializer.Deserialize<RootFolderResource>(bodyContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true});

            var model = rootFolderResource!.ToModel();
            return _rootFolderService.Add(model).Id;
        }

        [Route(HttpVerbs.Get, "/")]
        public Task<IList<RootFolderResource>> GetRootFoldersAsync()
        {
            var result =  _rootFolderService.AllWithUnmappedFolders().ToResource();
            return Task.FromResult<IList<RootFolderResource>>(result);
        }

        [Route(HttpVerbs.Delete, "/{id}")]
        private void DeleteFolderAsync(int id)
        {
            _rootFolderService.Remove(id);
        }
    }
}
