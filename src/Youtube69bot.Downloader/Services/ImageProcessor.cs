using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Youtube69bot.Downloader.Services;

public static class ImageProcessor
{
    public static async Task<string> ResizeToTelegramThumbnailSizeAndCompressAsync(string path)
    {
        using var image = await Image.LoadAsync(path);

        // Thumbnail image width or height can not be greater than 320
        var newWidth = 320;
        var newHeight = (int)(image.Height * ((double)newWidth / image.Width));
        if (newHeight > 320)
        {
            newHeight = 320;
            newWidth = (int)(image.Width * ((double)newHeight / image.Height));
        }

        image.Mutate(x => x.Resize(newWidth, newHeight));

        var jpegEncoder = new JpegEncoder()
        {
            Quality = 55
        };

        await image.SaveAsync(path, jpegEncoder);

        return path;
    }
}