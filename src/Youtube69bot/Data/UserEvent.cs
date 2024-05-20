namespace Youtube69bot.Data;

public class UserEvent
{
    public int Id { get; set; }
    public UserEventType EventType { get; set; }
    public DateTimeOffset DateEventHappened { get; set; }
    public long UserId { get; set; }
    public User User { get; set; }
}

public enum UserEventType
{
    Blocked,
    Unblocked,
    Unknown
}