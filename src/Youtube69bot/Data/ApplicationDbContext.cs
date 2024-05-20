using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Youtube69bot.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Channel> Channels { get; set; }
    public DbSet<MandatoryChannelsVersion> MandatoryChannelsVersions { get; set; }
    public DbSet<TextMessageToSend> TextMessageToSends { get; set; }
    public DbSet<UsersToSendMessage> UsersToSendMessages { get; set; }
    public DbSet<UserEvent> UserEvents { get; set; }
    public DbSet<UserJoinedChannel> UserJoinedChannels { get; set; }
    public DbSet<ResolvedYoutubeLink> ResolvedYoutubeLinks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}