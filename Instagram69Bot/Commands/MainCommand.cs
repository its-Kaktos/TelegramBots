using Instagram69Bot.Data;
using Instagram69Bot.Services;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Instagram69Bot.Data.User;

namespace Instagram69Bot.Commands;

public class MainCommand : BaseCommand
{
    public async Task HandleCommandAsync(ITelegramBotClient botClient, ApplicationDbContext dbContext, TelegramMessageService telegramMessageService, Message message, CancellationToken cancellationToken)
    {
        var user = await GetOrCreateUserAsync(dbContext, message.Chat.Id, message.From!.Id, cancellationToken);

        bool isUserInChannels;
        using (Operation.Time("Checking user is joined in channels"))
        {
            isUserInChannels = await IsUserStillJoinedInChannelsAsync(botClient, dbContext, user, cancellationToken);
        }

        if (isUserInChannels) await SetUserJoinedChannelToTrueAsync(dbContext, user);

        telegramMessageService.SendTextMessage(message, "برای دانلود، فقط کافیه لینک پست اینستاگرام رو اینجا بفرستی");
    }

    private async Task SetUserJoinedChannelToTrueAsync(ApplicationDbContext dbContext, User user)
    {
        user.IsInJoinedMandatoryChannels = true;

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();
    }
}