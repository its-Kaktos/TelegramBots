namespace Instagram69Bot.Data;

public class UserJoinedChannel
{
    public int Id { get; set; }
    public long ChannelId { get; set; }
    public Channel Channel { get; set; }
    public long UserChatId { get; set; }
    public User UserChat { get; set; }
    public DateTimeOffset JoinedDate { get; set; }
}