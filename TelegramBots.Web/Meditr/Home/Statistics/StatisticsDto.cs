namespace TelegramBots.Web.Meditr.Home.Statistics;

public class StatisticsDto
{
    public int UsersCount { get; set; }
    public int UsersBlockedBotCount { get; set; }
    public int UsersJoinedChannelsCount { get; set; }
    public int UsersJoinedChannelByBot { get; set; }
    public int UsersJoinedChannelsAndDidNotBlockBot { get; set; }

    public int UsersWhoWasJoinedChannelBeforeBot
    {
        get => UsersJoinedChannelsCount - UsersJoinedChannelByBot;
    }
}