using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Notifications;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Notifications
{
    [ApiController]
    [SonarrApiRoute("notification", RouteVersion.V3)]
    public class NotificationController : ProviderControllerBase<NotificationResource, INotification, NotificationDefinition>
    {
        private static readonly NotificationResourceMapper ResourceMapper = new();

        public NotificationController(NotificationFactory notificationFactory)
            : base(notificationFactory, ResourceMapper) { }

        protected override void Validate(NotificationDefinition definition, bool includeWarnings)
        {
            if (!definition.OnGrab && !definition.OnDownload) return;
            base.Validate(definition, includeWarnings);
        }
    }
}