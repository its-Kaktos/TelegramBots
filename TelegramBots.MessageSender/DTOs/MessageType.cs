namespace TelegramBots.MessageSender.DTOs;

public enum MessageType
{
    New,
    Update,
    Delete
}

public enum MessageContains
{
    Nothing = 0,
    TextMessage = 1,
    EditTextMessage = 2,
    GroupMedia = 3,
    Photo = 4
}