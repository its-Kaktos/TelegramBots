using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TelegramBots.MessageSender.DTOs;

namespace TelegramBots.MessageSender.Data;

public class InstagramCacheDbContext : DbContext, ICacheDbContext
{
    public InstagramCacheDbContext(DbContextOptions<InstagramCacheDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public DbSet<FileCacheInfo> FileCacheInfos { get; set; }
    public DbSet<VideoCache> VideoCaches { get; set; }
    public DbSet<AudioCache> AudioCaches { get; set; }

    public async Task<int> SaveChangesAsync()
    {
        return await base.SaveChangesAsync();
    }
}