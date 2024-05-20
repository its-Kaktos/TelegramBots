using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TelegramBots.MessageSender;

public static class PollingExtensions
{
    public static T GetConfiguration<T>(this IServiceProvider serviceProvider)
        where T : class
    {
        var o = serviceProvider.GetRequiredService<IOptions<T>>();

        return o.Value;
    }
}