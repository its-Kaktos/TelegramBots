using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TelegramBots.Web.Common.ExtensionMethods;

public static class HttpContextExtensions
{
    private static readonly ActionDescriptor EmptyActionDescriptor = new();

    public static Task WriteResultAsync<TResult>(this HttpContext context, TResult result)
        where TResult : IActionResult
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (result == null) throw new ArgumentNullException(nameof(result));

        var executor = context.RequestServices.GetService<IActionResultExecutor<TResult>>();

        if (executor == null) throw new InvalidOperationException($"No result executor for '{typeof(TResult).FullName}' has been registered.");

        var routeData = context.GetRouteData();

        var actionContext = new ActionContext(context, routeData, EmptyActionDescriptor);

        return executor.ExecuteAsync(actionContext, result);
    }
}