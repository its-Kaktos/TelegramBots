using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Youtube69bot.Data.Configurations;

public class UserJoinedChannelConfiguration : IEntityTypeConfiguration<UserJoinedChannel>
{
    public void Configure(EntityTypeBuilder<UserJoinedChannel> builder)
    {
        builder.HasOne(x => x.Channel)
            .WithMany(x => x.UserJoinedChannels)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UserChat)
            .WithMany(x => x.UserJoinedChannels)
            .OnDelete(DeleteBehavior.Restrict);
    }
}