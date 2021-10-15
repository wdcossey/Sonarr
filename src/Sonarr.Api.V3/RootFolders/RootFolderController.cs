﻿using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation.Paths;
using Sonarr.Api.V3;
using Sonarr.Api.V3.RootFolders;
using Sonarr.Http.Attributes;

namespace NzbDrone.Api.V3.RootFolders
{
    [ApiController]
    [SonarrApiRoute("rootFolder", RouteVersion.V3)]
    public class RootFolderController : ControllerBase
    {
        private readonly IRootFolderService _rootFolderService;
        //private readonly IEventAggregator _eventAggregator;

        public RootFolderController(IRootFolderService rootFolderService/*,
            IBroadcastSignalRMessage signalRBroadcaster*/, //TODO: SignalR Hub
            //IEventAggregator eventAggregator,
            RootFolderValidator rootFolderValidator,
            PathExistsValidator pathExistsValidator,
            MappedNetworkDriveValidator mappedNetworkDriveValidator,
            StartupFolderValidator startupFolderValidator,
            SystemFolderValidator systemFolderValidator,
            FolderWritableValidator folderWritableValidator
            )
        {
            _rootFolderService = rootFolderService;
            //_eventAggregator = eventAggregator;
            /*
            SharedValidator.RuleFor(c => c.Path)
                           .Cascade(CascadeMode.StopOnFirstFailure)
                           .IsValidPath()
                           .SetValidator(rootFolderValidator)
                           .SetValidator(mappedNetworkDriveValidator)
                           .SetValidator(startupFolderValidator)
                           .SetValidator(pathExistsValidator)
                           .SetValidator(systemFolderValidator)
                           .SetValidator(folderWritableValidator);*/
        }

        [HttpGet("{id:int}")]
        public IActionResult GetRootFolder(int id, [FromQuery] bool timeout = true)
            => Ok(_rootFolderService.Get(id, timeout).ToResource());

        [HttpPost]
        public IActionResult CreateRootFolder([FromBody] RootFolderResource rootFolderResource)
        {
            var model = _rootFolderService.Add(rootFolderResource.ToModel());
            return Created($"{Request.Path}/{model.Id}", model.ToResource());
        }

        [HttpGet]
        public IActionResult GetRootFolders()
            => Ok(_rootFolderService.AllWithUnmappedFolders().ToResource());

        [HttpDelete("{id:int}")]
        public IActionResult DeleteFolder(int id)
        {
            _rootFolderService.Remove(id);

            /*_eventAggregator.PublishEvent(new BroadcastMessageEvent() { Message = new SignalRMessage
            {
                Name = "rootFolder",
                Body = new ResourceChangeMessage<RootFolderResource>(ModelAction.Deleted),
                Action = ModelAction.Deleted
            }});*/
            return Ok(new object());
        }
    }
}