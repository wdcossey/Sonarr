using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Messaging.Events;
using Sonarr.Http.REST;

namespace Sonarr.Api.V3
{
    public abstract class SonarrControllerBase : ControllerBase
    {
        protected SonarrControllerBase() { }
    }

    public abstract class SonarrControllerBase<TResource> : SonarrControllerBase where TResource : RestResource, new()
    {
        protected SonarrControllerBase() { }

        [HttpGet]
        public virtual async Task<IActionResult> GetAllAsync()
            => Ok(await GetAllResourcesAsync());

        [HttpGet("{id:int}")]
        public virtual async Task<IActionResult> GetByIdAsync(int id)
            => Ok(await GetResourceByIdAsync(id));

        [HttpPost]
        public virtual async Task<IActionResult> CreateAsync([FromBody] TResource resource)
            => Created(Request.Path, await CreateResourceAsync(resource) ?? new object());

        [HttpPut("{id:int}")]
        public virtual Task<IActionResult> UpdateByIdAsync(int id, [FromBody] TResource resource)
        {
            resource.Id = id;
            return UpdateAsync(resource);
        }

        [HttpPut]
        public virtual async Task<IActionResult> UpdateAsync([FromBody] TResource resource)
            => Accepted(await UpdateResourceAsync(resource) ?? new object());

        [HttpDelete("{id:int}")]
        public virtual async Task<IActionResult> DeleteAsync(int id)
        {
            await DeleteResourceByIdAsync(id);
            return Ok(new object());
        }

        protected abstract Task<IList<TResource>> GetAllResourcesAsync();

        protected abstract Task<TResource> GetResourceByIdAsync(int id);

        protected abstract Task DeleteResourceByIdAsync(int id);

        protected abstract Task<TResource> UpdateResourceAsync([FromBody] TResource resource);

        protected abstract Task<TResource> CreateResourceAsync([FromBody] TResource resource);

    }

    public abstract class SonarrControllerBase<TResource, TModel> : SonarrControllerBase<TResource>, IHandle<ModelEvent<TModel>>
        where TResource : RestResource, new()
        where TModel : ModelBase, new()
    {

        //private readonly IBroadcastSignalRMessage _signalRBroadcaster;

        protected SonarrControllerBase(/*IBroadcastSignalRMessage signalRBroadcaster*/)
        {
            //_signalRBroadcaster = signalRBroadcaster;
        }

        public void Handle(ModelEvent<TModel> message)
        {
            /*if (!_signalRBroadcaster.IsConnected) return;

            if (message.Action == ModelAction.Deleted || message.Action == ModelAction.Sync)
            {
                BroadcastResourceChange(message.Action);
            }

            BroadcastResourceChange(message.Action, message.ModelId);*/
        }
    }
}
