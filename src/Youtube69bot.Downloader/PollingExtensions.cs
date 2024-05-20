using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Youtube69bot.Downloader;

public static class PollingExtensions
{
    public static T GetConfiguration<T>(this IServiceProvider serviceProvider)
        where T : class
    {
        var o = serviceProvider.GetRequiredService<IOptions<T>>();
        if (o is null)
            throw new ArgumentNullException(nameof(T));

        return o.Value;
    }
}