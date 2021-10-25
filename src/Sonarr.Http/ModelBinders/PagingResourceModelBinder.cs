using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using NzbDrone.Core.Datastore;

namespace Sonarr.Http.ModelBinders
{
    public class PagingResourceModelBinder : IModelBinder
    {
        private static readonly HashSet<string> EXCLUDED_KEYS = new(StringComparer.InvariantCultureIgnoreCase)
        {
            "page",
            "pageSize",
            "sortKey",
            "sortDirection",
            "filterKey",
            "filterValue",
        };

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            try
            {
                if (!typeof(PagingResource).IsAssignableFrom(bindingContext.ModelType))
                    throw new InvalidOperationException($"{bindingContext.ModelType} is not supported by {typeof(PagingResourceModelBinder)}");

                var pagingResource = (PagingResource)ActivatorUtilities.CreateInstance(
                    bindingContext.HttpContext.RequestServices.GetRequiredService<IServiceProvider>(),
                    bindingContext.ModelType);

                pagingResource.Filters = new List<PagingResourceFilter>();

                if (!int.TryParse(bindingContext.ValueProvider.GetValue("page").FirstValue, out var page)|| page < 1)
                    page = 1;

                if (!int.TryParse(bindingContext.ValueProvider.GetValue("pageSize").FirstValue, out var pageSize) || pageSize < 0)
                    pageSize = 10;

                pagingResource.Page = page;
                pagingResource.PageSize = pageSize;

                var sortKeyValueProvider = bindingContext.ValueProvider.GetValue("sortKey");
                if (sortKeyValueProvider != ValueProviderResult.None)
                {
                    pagingResource.SortKey = sortKeyValueProvider.FirstValue;

                    // For backwards compatibility with v2
                    var sortDirValueProvider = bindingContext.ValueProvider.GetValue("sortDir");
                    if (sortDirValueProvider != ValueProviderResult.None)
                    {
                        pagingResource.SortDirection = sortDirValueProvider.FirstValue
                            .Equals("Asc", StringComparison.InvariantCultureIgnoreCase)
                            ? SortDirection.Ascending
                            : SortDirection.Descending;
                    }

                    // v3 uses SortDirection instead of SortDir to be consistent with every other use of it
                    var sortDirectionValueProvider = bindingContext.ValueProvider.GetValue("sortDir");
                    if (sortDirectionValueProvider != ValueProviderResult.None)
                    {
                        pagingResource.SortDirection = Enum.Parse<SortDirection>(sortDirectionValueProvider.FirstValue, true);
                    }
                }

                // For backwards compatibility with v2
                var filterKeyValueProvider = bindingContext.ValueProvider.GetValue("filterKey");
                if (filterKeyValueProvider != ValueProviderResult.None)
                {
                    var filter = new PagingResourceFilter
                    {
                        Key = filterKeyValueProvider.FirstValue
                    };

                    var filterValueValueProvider = bindingContext.ValueProvider.GetValue("filterValue");
                    if (filterValueValueProvider != ValueProviderResult.None)
                    {
                        filter.Value = filterValueValueProvider.FirstValue;
                    }

                    var filters = pagingResource.Filters ??= new List<PagingResourceFilter>();

                    filters.Add(filter);
                }

                // v3 uses filters in key=value format
                foreach (var (queryKey, queryValues) in bindingContext.HttpContext.Request.Query.Where(w => !EXCLUDED_KEYS.Contains(w.Key)))
                {
                    var filters = pagingResource.Filters ??= new List<PagingResourceFilter>();

                    filters.Add(new PagingResourceFilter
                    {
                        Key = queryKey,
                        Value = queryValues
                    });
                }

                bindingContext.Result = ModelBindingResult.Success(pagingResource);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
