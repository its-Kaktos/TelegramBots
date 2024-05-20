using Instagram69Bot.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using Telegram.Bot;

namespace Instagram69Bot.Services;

public class BulkMessageSender
{
    private readonly ITelegramBotClient _botClient;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;

    public BulkMessageSender(ITelegramBotClient botClient, ApplicationDbContext dbContext, ILogger logger)
    {
        _botClient = botClient;
        _dbContext = dbContext;
        _logger = logger.ForContext<BulkMessageSender>();
    }

    public async Task SendNewBulkMessageAsync(List<long> chatIds, string messageText, long adminChatId)
    {
        using (LogContext.PushProperty("UsersCount", chatIds.Count))
        using (LogContext.PushProperty("MessageText", messageText))
        {
            try
            {
                _logger.Information("Start processing of bulk message send");
                var textMessageToSendId = await AddMessagesToDbAsync(chatIds, messageText);
                var isSuccessful = true;
                foreach (var chatId in chatIds)
                {
                    try
                    {
                        await _botClient.SendTextMessageAsync(chatId, messageText);
                        await RemoveChatIdFromQueueAsync(chatId, textMessageToSendId);

                        // Delay sending next message to avoid telegram bulk message sending limitation, 750ms should be more that enough.
                        await Task.Delay(750);
                    }
                    catch (Exception e)
                    {
                        isSuccessful = false;
                        _logger.ForContext("ChatId", chatId)
                            .Error(e, "Exception occured while sending bulk message to user");
                    }
                }

                if (!isSuccessful)
                {
                    await _botClient.SendTextMessageAsync(adminChatId,
                        "پیغام فرستاده شده به تعدادی از کاربران ارسال نشد. پیشنهاد میشود لاگ را بررسی کنید.");

                    _logger.Warning("Sent bulk message operation was not successful, some users did not get message");

                    return;
                }

                await SetStatusOfTextMessageToCompletedAsync(textMessageToSendId);

                await _botClient.SendTextMessageAsync(adminChatId,
                    "پیغام فرستاده شده با موفقیت به تمامی کاربران ارسال شد. پیشنهاد میشود لاگ را بررسی کنید.");

                _logger.Information("Sending bulk message completed");
            }
            catch (Exception e)
            {
                _logger.Error(e, "Exception occured while processing sending bulk messages");
                await _botClient.SendTextMessageAsync(adminChatId, "خطایی رخ داد، پیام ارسالی به کاربران ارسال نشد.");
            }
        }
    }

    public async Task SendRemainingBulkMessagesAsync(long adminChatId)
    {
        try
        {
            var textMessagesToSendIds = await GetUncompletedTextMessagesIdsAsync();

            foreach (var textMessagesToSendId in textMessagesToSendIds)
            {
                var chatIds = await GetChatIdsToSendMessageToAsync(textMessagesToSendId);
                var messageText = await GetMessageTextToSendToUsersAsync(textMessagesToSendId);

                using (LogContext.PushProperty("UsersCount", chatIds.Count))
                using (LogContext.PushProperty("MessageText", messageText))
                {
                    await _botClient.SendTextMessageAsync(adminChatId, "ارسال پیام فرستاده شده به تمامی کاربران از سر گرفته شد.");
                    _logger.Information("Start processing of remaining bulk message send");
                    var isSuccessful = true;
                    foreach (var chatId in chatIds)
                    {
                        try
                        {
                            await _botClient.SendTextMessageAsync(chatId, messageText);
                            await RemoveChatIdFromQueueAsync(chatId, textMessagesToSendId);

                            // Delay sending next message to avoid telegram bulk message sending limitation, 750ms should be more that enough.
                            await Task.Delay(750);
                        }
                        catch (Exception e)
                        {
                            isSuccessful = false;
                            _logger.ForContext("ChatId", chatId)
                                .Error(e, "Exception occured while sending bulk message to user");
                        }
                    }

                    if (!isSuccessful)
                    {
                        await _botClient.SendTextMessageAsync(adminChatId,
                            "پیغام فرستاده شده به تعدادی از کاربران ارسال نشد. پیشنهاد میشود لاگ را بررسی کنید.");

                        _logger.Warning("Re-sending bulk message operation was not successful, some users did not get message");

                        return;
                    }

                    await SetStatusOfTextMessageToCompletedAsync(textMessagesToSendId);

                    await _botClient.SendTextMessageAsync(adminChatId,
                        "پیغام فرستاده شده با موفقیت به تمامی کاربران ارسال شد. پیشنهاد میشود لاگ را بررسی کنید.");

                    _logger.Information("Re-sending bulk message completed");
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Exception occured while processing re-sending bulk messages");
        }
    }

    private async Task<string> GetMessageTextToSendToUsersAsync(int textMessageToSendId)
    {
        var data = await _dbContext.TextMessageToSends.AsNoTracking()
            .Where(x => x.Id == textMessageToSendId)
            .Select(x => x.MessageText)
            .FirstAsync();

        return data;
    }

    private async Task<List<int>> GetUncompletedTextMessagesIdsAsync()
    {
        var data = await _dbContext.TextMessageToSends.AsNoTracking()
            .Where(x => !x.IsCompleted)
            .Select(x => x.Id)
            .ToListAsync();

        return data;
    }

    private async Task<List<long>> GetChatIdsToSendMessageToAsync(int textMessageToSendId)
    {
        var data = await _dbContext.UsersToSendMessages.AsNoTracking()
            .Where(x => x.TextMessageToSendId == textMessageToSendId)
            .Select(x => x.UserId)
            .ToListAsync();

        return data;
    }

    private async Task SetStatusOfTextMessageToCompletedAsync(int textMessageToSenId)
    {
        var data = await _dbContext.TextMessageToSends
            .Where(x => x.Id == textMessageToSenId)
            .FirstAsync();

        data.IsCompleted = true;

        _dbContext.Update(data);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<int> AddMessagesToDbAsync(List<long> chatIds, string messageText)
    {
        var textMessageToSend = new TextMessageToSend
        {
            MessageText = messageText,
            IsCompleted = false
        };

        await _dbContext.TextMessageToSends.AddAsync(textMessageToSend);
        await _dbContext.SaveChangesAsync();

        var usersToSendMessages = new List<UsersToSendMessage>(chatIds.Count);
        foreach (var chatId in chatIds)
        {
            usersToSendMessages.Add(new UsersToSendMessage
            {
                UserId = chatId,
                TextMessageToSendId = textMessageToSend.Id
            });
        }

        await _dbContext.UsersToSendMessages.AddRangeAsync(usersToSendMessages);
        await _dbContext.SaveChangesAsync();

        return textMessageToSend.Id;
    }

    private async Task RemoveChatIdFromQueueAsync(long chatId, int textMessageToSendId)
    {
        var data = await _dbContext.UsersToSendMessages
            .Where(x => x.UserId == chatId && x.TextMessageToSendId == textMessageToSendId)
            .FirstOrDefaultAsync();

        if (data is null) return;

        _dbContext.Remove(data);
        await _dbContext.SaveChangesAsync();
    }
}