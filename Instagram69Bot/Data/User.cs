using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Instagram69Bot.Data;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long ChatId { get; set; }

    public long UserId { get; set; }
    public bool IsInJoinedMandatoryChannels { get; set; }

    public DateTimeOffset JoinedDate { get; set; }

    [Required]
    public int VersionUserJoinedId { get; set; }

    public MandatoryChannelsVersion? VersionUserJoined { get; set; }

    public bool IsBotBlocked { get; set; }

    public List<UserEvent> UserEvents { get; set; } = new();
    public List<UserJoinedChannel> UserJoinedChannels { get; set; } = new();
}