using System.Text.RegularExpressions;

namespace Youtube69bot.Downloader.Services;

public class LinkKeyGenerator
{
    private static readonly Regex YoutubeLinkKeyRegex = new Regex(@"^(?:https?:\/\/)?(?:(?:www|m)\.)?(?:(?:youtube\.com|youtu.be))(?:\/(?:[\w\-]+\?v=|embed\/|v\/)?)(?!.*playlist|channel|user|feed)([\w\-]+)(\S+)?$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1.5));

    public static string? Generate(string? link)
    {
        if (link is null) return null;

        var match = YoutubeLinkKeyRegex.Match(link);
        if (match.Groups.Count < 2) return null;

        return match.Groups[1].Value;
    }
}