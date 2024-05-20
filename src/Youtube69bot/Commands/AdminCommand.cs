using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using Telegram.Bot;
using Telegram.Bot.Types;
using Youtube69bot.Data;
using Youtube69bot.Services;

namespace Youtube69bot.Commands;

public class AdminCommand
{
    private readonly ILogger _logger;
    private readonly MainCommand _mainCommand;
    private readonly IServiceProvider _serviceProvider;

    public AdminCommand(ILogger logger, MainCommand mainCommand, IServiceProvider serviceProvider)
    {
        _logger = logger.ForContext<AdminCommand>();
        _mainCommand = mainCommand;
        _serviceProvider = serviceProvider;
    }

    public async Task HandleCommandAsync(ITelegramBotClient botClient, ApplicationDbContext dbContext, TelegramMessageService telegramMessageService, Message message, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("ChatId", message.Chat.Id))
        using (LogContext.PushProperty("UserId", message.From?.Id))
        {
            var adminIds = new long[]
            {
                445759465, 2139339416
            };
            if (!adminIds.Contains(message.From!.Id))
            {
                _logger.Warning("User tried to access ADMIN section");
                await _mainCommand.HandleCommandAsync(botClient, dbContext, telegramMessageService, message, cancellationToken);
            }

            // CHECK SEND MESSAGE TO ANOTHER USER.
            var command = message.Text!.Split(" ");
            switch (command[1])
            {
                case "sendmessage":
                    HandleSendMessageToUser(telegramMessageService, message, command);
                    break;
                case "sendmessagetoall":
                    await HandleSendMessageToَAllUsersAsync(telegramMessageService, dbContext, message, command, cancellationToken);
                    break;
                case "resenduncompletedmessages":
                    HandleReSendUncompletedMessages(telegramMessageService, message);
                    break;
            }
        }
    }

    private static void HandleSendMessageToUser(TelegramMessageService telegramMessageService, Message message, IReadOnlyList<string> command)
    {
        var chatId = command[2];
        var sb = new StringBuilder();
        for (var i = 3; i < command.Count; i++)
        {
            sb.Append(command[i]);
            sb.Append(' ');
        }

        var messageText = sb.ToString();

        telegramMessageService.SendTextMessage(long.Parse(chatId), messageText);

        telegramMessageService.SendTextMessage(message, "پیام مورد نظر ارسال شد");
    }

    private async Task HandleSendMessageToَAllUsersAsync(TelegramMessageService telegramMessageService, ApplicationDbContext dbContext, Message message, IReadOnlyList<string> command, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        for (var i = 2; i < command.Count; i++)
        {
            sb.Append(command[i]);
            sb.Append(' ');
        }

        var messageText = sb.ToString();

        var allChatIds = await dbContext.Users.AsNoTracking()
            .Where(u => !u.IsBotBlocked)
            .Select(u => u.ChatId)
            .ToListAsync(cancellationToken: cancellationToken);

        Task.Run(() =>
        {
            using var scope = _serviceProvider.CreateScope();
            var bulkMessageSender = scope.ServiceProvider.GetRequiredService<BulkMessageSender>();
            bulkMessageSender.SendNewBulkMessageAsync(allChatIds, messageText, message.From!.Id).GetAwaiter().GetResult();
        });

        var responseMessageText = $"پیام مورد نظر به کاربران ارسال خواهد شد. بسته به تعداد کاربران، ارسال پیام ممکن است تا ساعاتی طول بکشد. تعداد کل کاربران {allChatIds.Count} نفر.";

        telegramMessageService.SendTextMessage(message, responseMessageText);
    }

    private void HandleReSendUncompletedMessages(TelegramMessageService telegramMessageService, Message message)
    {
        Task.Run(() =>
        {
            using var scope = _serviceProvider.CreateScope();
            var bulkMessageSender = scope.ServiceProvider.GetRequiredService<BulkMessageSender>();
            bulkMessageSender.SendRemainingBulkMessagesAsync(message.From!.Id).GetAwaiter().GetResult();
        });

        var responseMessageText = "ارسال پیام های باقی مانده به کاربران از سر گرفته شد. ارسال پیام ها بسته به تعداد کاربران ممکن است چندین ساعت طول بکشد.";

        telegramMessageService.SendTextMessage(message, responseMessageText);
    }
}