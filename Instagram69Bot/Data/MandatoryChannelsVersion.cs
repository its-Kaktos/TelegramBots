namespace Instagram69Bot.Data;

public class MandatoryChannelsVersion
{
    public int Id { get; set; }
    public int Version { get; set; }
    public DateTimeOffset AddedDate { get; set; }
    public string? Description { get; set; }
    public List<Channel> Channels { get; set; } = new();
    public List<User> Users { get; set; } = new();
}