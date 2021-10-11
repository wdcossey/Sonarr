using System;
using NzbDrone.Core.Datastore;
using Sonarr.Http.REST;

namespace Sonarr.Http.Extensions
{
    public static class PagingExtensions
    {
        public static PagingResource<TResource> ApplyToPage<TModel, TResource>(this PagingSpec<TModel> pagingSpec, Func<PagingSpec<TModel>, PagingSpec<TModel>> function, Converter<TModel, TResource> mapper)
            where TResource : RestResource
        {
            pagingSpec = function(pagingSpec);

            return new PagingResource<TResource>
            {
                Page = pagingSpec.Page,
                PageSize = pagingSpec.PageSize,
                SortDirection = pagingSpec.SortDirection,
                SortKey = pagingSpec.SortKey,
                TotalRecords = pagingSpec.TotalRecords,
                Records = pagingSpec.Records.ConvertAll(mapper)
            };
        }
    }
}
