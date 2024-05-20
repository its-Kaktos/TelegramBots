using Microsoft.EntityFrameworkCore;
using Serilog;
using Youtube69bot.Data;

namespace Youtube69bot.Services;

public class SeedDatabaseService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;

    public SeedDatabaseService(ApplicationDbContext dbContext, ILogger logger)
    {
        _dbContext = dbContext;
        _logger = logger.ForContext<SeedDatabaseService>() ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
        _logger.Debug("Database has been migrated");

        var isAnythingInDb = await _dbContext.Channels.AnyAsync(cancellationToken: cancellationToken);
        if (!isAnythingInDb)
        {
            _logger.Debug("Starting seeding database");
            await _dbContext.Channels.AddAsync(new Channel
            {
                ChannelId = -1001299867276,
                ChannelName = "69;",
                ChannelJoinLink = "https://t.me/+q5c1RRf_iUhlMzk0",
                IsNotAllowedToLeaveChannel = true,
                UsersJoinedFromBot = 0,
                Version = new MandatoryChannelsVersion
                {
                    Version = 1,
                    AddedDate = DateTimeOffset.Now,
                    Description = "First version, auto added from application"
                }
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.Debug("Seeding database finished successfully");
        }
    }
}