using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Serilog;

namespace TelegramBots.Web.Common.MediatrBehaviours
{
    public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly ILogger _logger;
        private readonly Stopwatch _timer;

        public PerformanceBehavior(ILogger logger)
        {
            _timer = new Stopwatch();

            _logger = logger.ForContext<PerformanceBehavior<TRequest, TResponse>>();
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _timer.Start();

            var response = await next();

            _timer.Stop();

            var elapsedMilliseconds = _timer.ElapsedMilliseconds;

            if (elapsedMilliseconds > 1_000)
            {
                _logger
                    .ForContext("RequestFullName", typeof(TRequest).FullName)
                    .ForContext("ExecutionTimeInMS", elapsedMilliseconds.ToString())
                    .Warning("Long Running Request");
            }

            return response;
        }
    }
}