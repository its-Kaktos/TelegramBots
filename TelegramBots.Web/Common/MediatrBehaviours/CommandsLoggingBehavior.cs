using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Serilog;

namespace TelegramBots.Web.Common.MediatrBehaviours
{
    public class CommandsLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly ILogger _logger;

        public CommandsLoggingBehavior(ILogger logger)
        {
            _logger = logger.ForContext<CommandsLoggingBehavior<TRequest, TResponse>>();
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = await next();

            // By convention used in this project, if a request name
            // ends with "Command", then its (obviously) a command.
            if (typeof(TRequest).Name.EndsWith("Command", StringComparison.OrdinalIgnoreCase))
            {
                _logger
                    .ForContext("CommandFullName", typeof(TRequest).FullName)
                    .Information("An command was executed successfully");
            }

            return response;
        }
    }
}