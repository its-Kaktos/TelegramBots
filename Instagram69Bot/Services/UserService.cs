using Instagram69Bot.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Instagram69Bot.Services;

public class UserService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;

    public UserService(ApplicationDbContext dbContext, ILogger logger)
    {
        _dbContext = dbContext;
        _logger = logger.ForContext<UserService>();
    }

    public async Task BlockedBotAsync(long chatId, DateTimeOffset dateBlocked, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Where(x => x.ChatId == chatId)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        // ChatId might not exists in DB because we dont save channels in our db but channels can kick our bot too.
        if (user is null)
        {
            _logger.Information("User chat member updated but user not found in DB");
            return;
        }

        user.IsBotBlocked = true;

        var userEvent = new UserEvent
        {
            EventType = UserEventType.Blocked,
            DateEventHappened = dateBlocked,
            UserId = chatId
        };

        _dbContext.Users.Update(user);
        await _dbContext.UserEvents.AddAsync(userEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UnBlockedBotAsync(long chatId, DateTimeOffset dateUnblocked, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Where(x => x.ChatId == chatId)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        // ChatId might not exists in DB because we dont save channels in our db but channels can kick our bot too.
        if (user is null)
        {
            _logger.Information("User chat member updated but user not found in DB");
            return;
        }

        user.IsBotBlocked = false;

        var userEvent = new UserEvent
        {
            EventType = UserEventType.Unblocked,
            DateEventHappened = dateUnblocked,
            UserId = chatId
        };

        _dbContext.Users.Update(user);
        await _dbContext.UserEvents.AddAsync(userEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}