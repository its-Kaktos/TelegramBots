using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TelegramBots.Web.Middlewares;

public static class CustomExceptionLoggerHandlerMiddlewareExtensions
{
    /// <summary>
    /// <para>
    /// If environment is not development, will log the exception and change the response status code
    /// to 500.
    /// </para>
    /// <para>
    /// If environment is development, will re-throw the exception so the UseDeveloperExceptionPage
    /// middleware can handle the exception and show it to the developer.
    /// </para>
    /// <para>
    /// Important : The recommended pipeline configuration is : First Use UseCustomStatusCodeHandler and then UseCustomExceptionLoggerHandler.
    /// </para>
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
    public static IApplicationBuilder UseCustomExceptionLoggerHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CustomExceptionLoggerHandlerMiddleware>();
    }
}

public class CustomExceptionLoggerHandlerMiddleware
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CustomExceptionLoggerHandlerMiddleware> _logger;
    private readonly RequestDelegate _next;

    public CustomExceptionLoggerHandlerMiddleware(RequestDelegate next,
        IWebHostEnvironment env,
        ILogger<CustomExceptionLoggerHandlerMiddleware> logger)
    {
        _next = next;
        _env = env;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            // If env is development, re-throw the exception so the UseDeveloperExceptionPage middleware can handle it.
            if (_env.IsDevelopment()) throw;

            _logger.LogError(exception, "An unhandled exception has occurred while executing the request");

            UpdateResponseStatusCode();
        }

        void UpdateResponseStatusCode()
        {
            if (context.Response.HasStarted)
                throw new InvalidOperationException("The response has already started, the CustomExceptionLoggerHandlerMiddleware will not be executed.");

            context.Response.StatusCode = 500;
        }
    }
}