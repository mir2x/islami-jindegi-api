using Microsoft.AspNetCore.Mvc.Filters;

namespace IslamiJindegiApi.Filters;

/// Applies one public pagination contract to every controller action which
/// accepts the conventional `page` / `pageSize` query arguments.
public sealed class PageSizeClampFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("page", out var page) && page is int pageNumber)
            context.ActionArguments["page"] = Math.Max(1, pageNumber);

        if (context.ActionArguments.TryGetValue("pageSize", out var pageSize) && pageSize is int size)
            context.ActionArguments["pageSize"] = Math.Clamp(size, 1, 100);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
