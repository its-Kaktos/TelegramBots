using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using TelegramBots.Web.Common.ExtensionMethods;

namespace TelegramBots.Web.Middlewares;

public static class CustomStatusCodeHandlerMiddlewareExtensions
{
    /// <summary>
    /// If status code is 404, will return a custom view.
    /// If status code is 500 and environment is not development, will show a custom view.
    /// <para>
    /// Important : The recommended pipeline configuration is : First Use UseCustomStatusCodeHandler and then UseCustomExceptionLoggerHandler.
    /// </para>
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
    public static IApplicationBuilder UseCustomStatusCodeHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CustomStatusCodeHandlerMiddleware>();
    }
}

public class CustomStatusCodeHandlerMiddleware
{
    private readonly IWebHostEnvironment _env;
    private readonly RequestDelegate _next;

    public CustomStatusCodeHandlerMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            ViewResult? result = context.Response.StatusCode switch
            {
                500 when !_env.IsDevelopment() => new ViewResult { ViewName = "~/Views/Shared/StatusCodes/StatusCode500.cshtml", StatusCode = 500 },
                _ => null
            };

            if (result != null) return context.WriteResultAsync(result);

            return Task.CompletedTask;
        });

        await _next(context);
    }
}