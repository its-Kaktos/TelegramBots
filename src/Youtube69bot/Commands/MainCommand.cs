using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Types;
using Youtube69bot.Data;
using Youtube69bot.Services;
using User = Youtube69bot.Data.User;

namespace Youtube69bot.Commands;

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
        telegramMessageService.SendTextMessage(message, "برای دانلود، فقط کافیه لینک ویدیو یوتیوب رو اینجا بفرستی");
    }

    private async Task SetUserJoinedChannelToTrueAsync(ApplicationDbContext dbContext, User user)
    {
        user.IsInJoinedMandatoryChannels = true;

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();
    }
}