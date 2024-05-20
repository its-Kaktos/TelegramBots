using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using TelegramBots.MessageSender.Data;
using TelegramBots.MessageSender.DTOs;

namespace TelegramBots.MessageSender.Services;

public class FileCacheService
{
    private const string ThumbnailPathDirectory = "/tmp/cache/thumbnails/";

    private string CopyThumbnailToCache(string thumbnailPath)
    {
        if (thumbnailPath.Contains(ThumbnailPathDirectory))
        {
            return thumbnailPath;
        }

        var thumbnailFilename = Path.GetFileName(thumbnailPath);
        var cachedThumbnailPath = Path.Combine(CreateThumbnailDateDirectory(), thumbnailFilename);
        System.IO.File.Copy(thumbnailPath, cachedThumbnailPath);

        return cachedThumbnailPath;

        string CreateThumbnailDateDirectory()
        {
            var directoryName = DateTime.Now.ToUniversalTime().ToString("yyyy/MM/dd/");
            var directoryPath = Path.Combine(ThumbnailPathDirectory, directoryName);
            Directory.CreateDirectory(directoryPath);

            return directoryPath;
        }
    }

    public async Task<FileCacheInfo> CreateAudioCacheAsync(ICacheDbContext dbContext, Audio audio,
        string userSentLink,
        string userSentLinkKey,
        float quality,
        string? thumbnailPath)
    {
        if (thumbnailPath is not null)
        {
            thumbnailPath = CopyThumbnailToCache(thumbnailPath);
        }

        var audioCache = new AudioCache
        {
            Title = audio.Title!,
            Duration = audio.Duration,
            ThumbnailPath = thumbnailPath,
            Quality = quality,
            FileId = audio.FileId
        };

        var fileCacheInfo = await dbContext.FileCacheInfos
            .FirstOrDefaultAsync(x => x.UserSentLinkKey == userSentLinkKey || x.UserSentLink == userSentLink);

        if (fileCacheInfo is null)
        {
            fileCacheInfo = new FileCacheInfo()
            {
                UserSentLink = userSentLink,
                UserSentLinkKey = userSentLinkKey,
                Type = FileCacheType.Audio,
                Audios = new List<AudioCache>
                {
                    audioCache
                }
            };

            await dbContext.FileCacheInfos.AddAsync(fileCacheInfo);
            await dbContext.SaveChangesAsync();

            return fileCacheInfo;
        }

        audioCache.FileCacheInfoId = fileCacheInfo.Id;
        fileCacheInfo.Type = fileCacheInfo.Type is FileCacheType.Video or FileCacheType.VideosAndAudios
            ? FileCacheType.VideosAndAudios
            : FileCacheType.Audio;

        await dbContext.AudioCaches.AddAsync(audioCache);
        dbContext.FileCacheInfos.Update(fileCacheInfo);
        await dbContext.SaveChangesAsync();

        return fileCacheInfo;
    }

    public async Task<FileCacheInfo> CreateVideoCacheAsync(ICacheDbContext dbContext,
        Video video,
        string userSentLink,
        string userSentLinkKey,
        int durationInSeconds,
        int width,
        int height,
        string? thumbnailPath)
    {
        if (thumbnailPath is not null)
        {
            thumbnailPath = CopyThumbnailToCache(thumbnailPath);
        }

        var videoCache = new VideoCache()
        {
            Duration = durationInSeconds,
            ThumbnailPath = thumbnailPath,
            FileId = video.FileId,
            Width = width,
            Height = height
        };

        var fileCacheInfo = await dbContext.FileCacheInfos
            .FirstOrDefaultAsync(x => x.UserSentLinkKey == userSentLinkKey || x.UserSentLink == userSentLink);

        if (fileCacheInfo is null)
        {
            fileCacheInfo = new FileCacheInfo()
            {
                UserSentLink = userSentLink,
                UserSentLinkKey = userSentLinkKey,
                Type = FileCacheType.Video,
                Videos = new List<VideoCache>
                {
                    videoCache
                }
            };

            await dbContext.FileCacheInfos.AddAsync(fileCacheInfo);
            await dbContext.SaveChangesAsync();

            return fileCacheInfo;
        }

        videoCache.FileCacheInfoId = fileCacheInfo.Id;
        fileCacheInfo.Type = fileCacheInfo.Type is FileCacheType.Audio or FileCacheType.VideosAndAudios
            ? FileCacheType.VideosAndAudios
            : FileCacheType.Video;

        await dbContext.VideoCaches.AddAsync(videoCache);
        dbContext.FileCacheInfos.Update(fileCacheInfo);
        await dbContext.SaveChangesAsync();

        return fileCacheInfo;
    }
}