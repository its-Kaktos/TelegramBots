using System;
using TelegramBots.Web.Common.ExtensionMethods;

namespace TelegramBots.Web.Meditr.UsersChart.GetUserJoinedBotPerDay;

public class GetUserJoinedBotPerDayDto
{
    public required DateTimeOffset Date { get; set; }

    public string DatePersian => Date.ToLocalTime().ToShamsiWithoutTime();

    public required int Count { get; set; }
}