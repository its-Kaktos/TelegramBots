namespace Instagram69Bot.Data;

public class TextMessageToSend
{
    public int Id { get; set; }
    public required string MessageText { get; set; }
    public bool IsCompleted { get; set; }
}