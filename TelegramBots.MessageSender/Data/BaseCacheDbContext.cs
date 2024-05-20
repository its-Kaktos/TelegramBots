using Microsoft.EntityFrameworkCore;
using TelegramBots.MessageSender.DTOs;

namespace TelegramBots.MessageSender.Data;

public interface ICacheDbContext
{
    public DbSet<FileCacheInfo> FileCacheInfos { get; set; }
    public DbSet<VideoCache> VideoCaches { get; set; }
    public DbSet<AudioCache> AudioCaches { get; set; }

    Task<int> SaveChangesAsync();
}