using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Youtube69bot.Data;

public class Channel
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long ChannelId { get; set; }

    public string ChannelName { get; set; }
    public string ChannelJoinLink { get; set; }
    public bool IsNotAllowedToLeaveChannel { get; set; }

    [Required]
    public int VersionId { get; set; }

    public MandatoryChannelsVersion? Version { get; set; }

    public int UsersJoinedFromBot { get; set; }

    public List<UserJoinedChannel> UserJoinedChannels { get; set; }
}