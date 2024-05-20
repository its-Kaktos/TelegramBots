namespace Youtube69bot.Data;

public class UsersToSendMessage
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public User User { get; set; }
    public int TextMessageToSendId { get; set; }
    public TextMessageToSend TextMessageToSend { get; set; }
}