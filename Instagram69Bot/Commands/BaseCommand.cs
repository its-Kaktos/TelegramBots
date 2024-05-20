using Instagram69Bot.Data;
using Instagram69Bot.Services;
using Instagram69Bot.Shared.MessageSender;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Instagram69Bot.Commands;

public abstract class BaseCommand
{
    protected async Task<bool> IsUserStillJoinedInChannelsAsync(ITelegramBotClient botClient, ApplicationDbContext dbContext, User user, CancellationToken cancellationToken = default)
    {
        if (!user.IsInJoinedMandatoryChannels) return false;

        var mandatoryChannelsUserNeedToJoin = await dbContext.Channels.AsNoTracking()
            .Where(c => c.VersionId == user.VersionUserJoinedId)
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (var channel in mandatoryChannelsUserNeedToJoin)
        {
            var chatMember = await botClient.GetChatMemberAsync(chatId: channel.ChannelId,
                userId: user.UserId,
                cancellationToken: cancellationToken);

            var isUserInChat = chatMember.Status switch
            {
                ChatMemberStatus.Creator or ChatMemberStatus.Administrator or
                    ChatMemberStatus.Member or ChatMemberStatus.Restricted => true,
                _ => false
            };

            if (isUserInChat) continue;

            return false;
        }

        return true;
    }

    protected async Task<User> GetOrCreateUserAsync(ApplicationDbContext applicationDbContext, long chatId, long userId, CancellationToken cancellationToken = default)
    {
        var user = await applicationDbContext.Users.AsNoTracking()
            .Where(u => u.ChatId == chatId)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (user is null)
        {
            var latestMandatoryChannelsVersionId = await applicationDbContext.MandatoryChannelsVersions.AsNoTracking()
                .MaxAsync(m => m.Id, cancellationToken: cancellationToken);

            user = new User
            {
                ChatId = chatId,
                UserId = userId,
                IsInJoinedMandatoryChannels = false,
                VersionUserJoinedId = latestMandatoryChannelsVersionId,
                JoinedDate = DateTimeOffset.Now
            };

            await applicationDbContext.Users.AddAsync(user, cancellationToken);
            await applicationDbContext.SaveChangesAsync(cancellationToken);
        }

        return user;
    }

    protected async Task SetUserJoinedChannelsToFalseAsync(ApplicationDbContext dbContext, User user, CancellationToken cancellationToken = default)
    {
        user.IsInJoinedMandatoryChannels = false;

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    protected async Task SendUserMandatoryChannelsToJoinAsync(TelegramMessageService telegramMessageService,
        ApplicationDbContext dbContext,
        User user,
        CancellationToken cancellationToken = default)
    {
        var mandatoryChannels = await dbContext.Channels.AsNoTracking()
            .Where(c => c.VersionId == user.VersionUserJoinedId)
            .ToListAsync(cancellationToken: cancellationToken);

        var keyboard = new List<IEnumerable<TelegramKeyboardButton>>();

        foreach (var channel in mandatoryChannels)
        {
            keyboard.Add(new[]
            {
                new TelegramKeyboardButton
                {
                    Text = channel.ChannelName,
                    Url = channel.ChannelJoinLink
                }
            });
        }

        keyboard.Add(new[]
        {
            new TelegramKeyboardButton
            {
                Text = "✅عضو شدم",
                CallbackData = "check_user_joined_channel"
            }
        });

        telegramMessageService.SendTextMessage(user.ChatId, "دوستم برای حمایت از ربات، باید عضو کانال زیر بشی",
            keyboard);
    }
}