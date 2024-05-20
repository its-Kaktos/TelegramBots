using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Serilog;
using Youtube69bot.Data;

namespace Youtube69bot.Quartz.Jobs;

[DisallowConcurrentExecution]
public class RemoveExpiredLinksFromDbJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public RemoveExpiredLinksFromDbJob(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger.ForContext<RemoveExpiredLinksFromDbJob>();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger
            .ForContext("JobName", nameof(RemoveExpiredLinksFromDbJob))
            .Debug("Start job");

        var expirationTime = DateTimeOffset.UtcNow - TimeSpan.FromHours(24);
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var expiredLinks = await dbContext.ResolvedYoutubeLinks.AsNoTracking()
            .Where(x => x.AddedDate < expirationTime)
            .ToListAsync();

        await dbContext.ResolvedYoutubeLinks.BulkDeleteAsync(expiredLinks);

        _logger
            .ForContext("JobName", nameof(RemoveExpiredLinksFromDbJob))
            .Debug("Finished job");
    }
}