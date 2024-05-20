using System.Drawing;
using FFMpegCore;
using FFMpegCore.Enums;
using Serilog;
using SerilogTimings;

namespace Instagram69bot.Downloader.Services;

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

        using (Operation.Time("Saving thumbnail of {videoPath} to disk", videoPath))
        {
            await FFMpeg.SnapshotAsync(input: videoPath,
                output: thumbnailPath,
                size,
                captureTime: TimeSpan.FromSeconds(1.5));
        }

        return thumbnailPath;
    }

    public async Task<string> ToMp3FormatAsync(string input)
    {
        var resultPath = Path.Combine(BaseFilePath, $"{Guid.NewGuid()}.mp3");

        // Add cover art to music file
        // using (Process p = new Process())
        // {
        //     p.StartInfo.UseShellExecute = false;
        //     p.StartInfo.CreateNoWindow = true;
        //     p.StartInfo.RedirectStandardOutput = true;
        //     p.StartInfo.FileName = "ffmpeg.exe";
        //     p.StartInfo.Arguments = "-i \"" + musicFile + "\" -i  \"" + albumInfo.Image.Uri.ToString() +
        //                             "\" -map 0:a -map 1 -codec copy -metadata:s:v title=\"Album cover\" -metadata:s:v comment=\"Cover (front)\" -disposition:v attached_pic \"" +
        //                             directoryFile + "\\" + title + " - " + albumArtist + "." + formatName + "\"";
        //     p.Start();
        //     p.WaitForExit();
        // }


        await FFMpegArguments
            .FromFileInput(input)
            .OutputToFile(resultPath, false, options =>
            {
                options.ForceFormat("mp3")
                    .WithAudioBitrate(AudioQuality.VeryHigh)
                    .WithAudioCodec(AudioCodec.LibMp3Lame);
            }).ProcessAsynchronously();

        return resultPath;
    }
}