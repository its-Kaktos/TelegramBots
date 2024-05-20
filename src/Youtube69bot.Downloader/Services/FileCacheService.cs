using Youtube69bot.Downloader.Dapper;
using Youtube69bot.Downloader.DTOs;

namespace Youtube69bot.Downloader.Services;

public class FileCacheService
{
    private readonly TelegramMessageService _telegramMessageService;

    public FileCacheService(TelegramMessageService telegramMessageService)
    {
        _telegramMessageService = telegramMessageService;
    }

    public async Task<bool> TrySendFileByCacheAsync(YoutubeFileCacheService cacheService, LinkResolverMessageDto messageDto)
    {
        // Get cache details from db
        var cacheResult = await GetInfoFromCacheAsync(cacheService, messageDto.YoutubeLink!);
        if (cacheResult is null) return false;

        if (messageDto.AudioQuality is not null)
        {
            return TrySendAudioCacheIfAvailable(messageDto, cacheResult);
        }

        if (messageDto.VideoHeight is null && messageDto.VideoWidth is null) return false;

        return TrySendVideoCacheIfAvailable(messageDto, cacheResult);
    }

    private bool TrySendVideoCacheIfAvailable(LinkResolverMessageDto messageDto, FileCacheInfo cacheResult)
    {
        if (cacheResult.Videos.Count == 0) return false;

        var selectedVideo = cacheResult.Videos
            .Find(x => x.Height == messageDto.VideoHeight!.Value);

        if (selectedVideo is null)
        {
            selectedVideo = cacheResult.Videos
                .Find(x => x.Width == messageDto.VideoWidth!.Value);
        }

        if (selectedVideo is null) return false;

        // Send video cache
        _telegramMessageService.SendCachedMediaGroup(messageDto.TelegramChatId, CreateCaption(),
            messageDto.YoutubeLink!, null, new CachedVideo
            {
                FileId = selectedVideo.FileId,
                ThumbnailPath = selectedVideo.ThumbnailPath,
                Duration = selectedVideo.Duration,
                Height = selectedVideo.Height,
                Width = selectedVideo.Width
            });

        return true;
    }

    private bool TrySendAudioCacheIfAvailable(LinkResolverMessageDto messageDto, FileCacheInfo cacheResult)
    {
        if (cacheResult.Audios.Count == 0) return false;


        var selectedAudio = cacheResult.Audios
            .Find(x => Math.Abs(x.Quality - messageDto.AudioQuality!.Value) < 1);

        if (selectedAudio is null)
        {
            selectedAudio = cacheResult.Audios
                .Find(x => Math.Abs((x.Quality + 10) - messageDto.AudioQuality!.Value) < 1
                           || Math.Abs((x.Quality - 10) - messageDto.AudioQuality.Value) < 1);
        }

        if (selectedAudio is null) return false;

        // Send audio cache
        _telegramMessageService.SendCachedMediaGroup(messageDto.TelegramChatId, CreateCaption(),
            messageDto.YoutubeLink!, new CachedAudio()
            {
                FileId = selectedAudio.FileId,
                Title = selectedAudio.Title,
                ThumbnailPath = selectedAudio.ThumbnailPath,
                Quality = selectedAudio.Quality,
                Duration = selectedAudio.Duration
            }, null);

        return true;
    }

    private static string CreateCaption()
    {
        const string aloneTag = "Downloaded via @DownloadYoutube69_bot";
        return aloneTag;
    }

    private async Task<FileCacheInfo?> GetInfoFromCacheAsync(YoutubeFileCacheService cacheService, string youtubeLink)
    {
        return await cacheService.GetFileCacheInfoAsync(youtubeLink);
    }
}