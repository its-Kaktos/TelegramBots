using System.Drawing;
using FFMpegCore;
using FFMpegCore.Enums;
using Serilog;
using SerilogTimings;

namespace Youtube69bot.Downloader.Services;

public class FFMpegWrapper
{
    private readonly ILogger _logger;
    private static readonly string BaseFilePath = Path.Combine(Path.GetTempPath(), "saved-thumbnails/");

    static FFMpegWrapper()
    {
        GlobalFFOptions.Configure(new FFOptions
        {
            BinaryFolder = "/usr/bin",
            TemporaryFilesFolder = "/tmp",
            LogLevel = FFMpegLogLevel.Debug
        });

        Directory.CreateDirectory(BaseFilePath);
    }

    public FFMpegWrapper(ILogger logger)
    {
        _logger = logger.ForContext<FFMpegWrapper>();
    }

    public async Task<(TimeSpan duration, int? height, int? width)> GetVideoDataAsync(string videoPath, CancellationToken cancellationToken = default)
    {
        IMediaAnalysis mediaAnalysis;
        using (Operation.Time("FFProbe Async analysis of {videoPath}", videoPath))
        {
            mediaAnalysis = await FFProbe.AnalyseAsync(videoPath, cancellationToken: cancellationToken);
        }

        return (mediaAnalysis.Duration, mediaAnalysis.PrimaryVideoStream?.Height, mediaAnalysis.PrimaryVideoStream?.Width);
    }

    public async Task<string> GetThumbnailAsync(string videoPath, int? height, int? width, CancellationToken cancellationToken = default)
    {
        var thumbnailPath = Path.Combine(BaseFilePath, $"{Guid.NewGuid()}.png");
        Size? size = null;
        if (height is not null && width is not null)
        {
            size = new Size((int)width, (int)height);
        }

        using (Operation.Time("Saving thumbnail of {videoPath} to disk, thumbnail {ThumbnailPath}", videoPath, thumbnailPath))
        {
            await FFMpeg.SnapshotAsync(input: videoPath,
                output: thumbnailPath,
                size,
                captureTime: TimeSpan.FromSeconds(1.5));
        }

        return thumbnailPath;
    }

    public async Task<(string, TimeSpan)> ToMp3FormatAsync(string audioPath)
    {
        var resultPath = Path.Combine(BaseFilePath, $"{Guid.NewGuid()}.mp3");

        var totalOperation = Operation.Time("Convert and analyze audio");
        using (Operation.Time("Convert to mp3"))
        {
            await FFMpegArguments
                .FromFileInput(audioPath)
                .OutputToFile(resultPath, false, options =>
                {
                    options.ForceFormat("mp3")
                        .WithAudioBitrate(AudioQuality.VeryHigh)
                        .WithAudioCodec(AudioCodec.LibMp3Lame);
                }).ProcessAsynchronously();
        }


        using (Operation.Time("Analyze audio"))
        {
            var analyze = await FFProbe.AnalyseAsync(resultPath);

            totalOperation.Dispose();

            return (resultPath, analyze.Duration);
        }
    }
}