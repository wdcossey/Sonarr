using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NzbDrone.Core.Datastore;
using Sonarr.Http;
using Sonarr.Http.REST;

namespace Sonarr.Api.V3
{
    public abstract class SonarrPagedController<TResource> : ControllerBase /*RestModule<TResource>*/ where TResource : RestResource, new()
    {

    }

    //TODO: Move and complete this mess!
    public class PagingResourceFilterAttribute : Attribute, IActionFilter
    {
        private readonly HashSet<string> EXCLUDED_KEYS = new(StringComparer.InvariantCultureIgnoreCase)
        {
            "page",
            "pageSize",
            "sortKey",
            "sortDirection",
            "filterKey",
            "filterValue",
        };

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var (_, value) = context.ActionArguments.FirstOrDefault(fd => fd.Value is PagingResource);

            if (value is null)
                return;

            /*int pageSize;
            int.TryParse(Request.Query.PageSize.ToString(), out pageSize);
            if (pageSize == 0) pageSize = 10;

            int page;
            int.TryParse(Request.Query.Page.ToString(), out page);
            if (page == 0) page = 1;


            var pagingResource = new PagingResource<TResource>
            {
                PageSize = pageSize,
                Page = page,
                Filters = new List<PagingResourceFilter>()
            };

            if (Request.Query.SortKey != null)
            {
                pagingResource.SortKey = Request.Query.SortKey.ToString();

                // For backwards compatibility with v2
                if (Request.Query.SortDir != null)
                {
                    pagingResource.SortDirection = Request.Query.SortDir.ToString()
                                                          .Equals("Asc", StringComparison.InvariantCultureIgnoreCase)
                                                       ? SortDirection.Ascending
                                                       : SortDirection.Descending;
                }

                // v3 uses SortDirection instead of SortDir to be consistent with every other use of it
                if (Request.Query.SortDirection != null)
                {
                    pagingResource.SortDirection = Request.Query.SortDirection.ToString()
                                                          .Equals("ascending", StringComparison.InvariantCultureIgnoreCase)
                                                       ? SortDirection.Ascending
                                                       : SortDirection.Descending;
                }
            }

            // For backwards compatibility with v2
            if (Request.Query.FilterKey != null)
            {
                var filter = new PagingResourceFilter
                             {
                                 Key = Request.Query.FilterKey.ToString()
                             };

                if (Request.Query.FilterValue != null)
                {
                    filter.Value = Request.Query.FilterValue?.ToString();
                }

                pagingResource.Filters.Add(filter);
            }

            // v3 uses filters in key=value format

            foreach (var key in Request.Query)
            {
                if (EXCLUDED_KEYS.Contains(key))
                {
                    continue;
                }

                pagingResource.Filters.Add(new PagingResourceFilter
                                           {
                                               Key = key,
                                               Value = Request.Query[key]
                                           });
            }*/

            // v3 uses filters in key=value format

            foreach (var (queryKey, queryValues) in context.HttpContext.Request.Query)
            {
                if (EXCLUDED_KEYS.Contains(queryKey))
                    continue;

                var filters = ((dynamic)value).Filters ??= new List<PagingResourceFilter>();

                filters.Add(new PagingResourceFilter()
                {
                    Key = queryKey,
                    Value = queryValues
                });
            }

        }

        public void OnActionExecuted(ActionExecutedContext context)
        {

        }
    }
}