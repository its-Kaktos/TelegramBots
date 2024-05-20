using Microsoft.EntityFrameworkCore;
using Serilog;
using TelegramBots.MessageSender.Data;

namespace TelegramBots.MessageSender.Services;

public class MigrateDatabaseService
{
    private readonly ILogger _logger;
    private readonly YoutubeCacheDbContext _youtubeCacheDbContext;
    private readonly InstagramCacheDbContext _instagramCacheDbContext;

    public MigrateDatabaseService(ILogger logger, YoutubeCacheDbContext youtubeCacheDbContext, InstagramCacheDbContext instagramCacheDbContext)
    {
        _logger = logger.ForContext<MigrateDatabaseService>() ?? throw new ArgumentNullException(nameof(logger));
        _youtubeCacheDbContext = youtubeCacheDbContext;
        _instagramCacheDbContext = instagramCacheDbContext;
    }

    public async Task MigrateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _youtubeCacheDbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
        _logger.Debug("Youtube cache database has been migrated");

        await _instagramCacheDbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
        _logger.Debug("Instagram cache database has been migrated");
    }
}