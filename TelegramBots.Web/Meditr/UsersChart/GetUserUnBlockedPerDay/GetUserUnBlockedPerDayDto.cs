using System;
using TelegramBots.Web.Common.ExtensionMethods;

namespace TelegramBots.Web.Meditr.UsersChart.GetUserUnBlockedPerDay;

public class GetUserUnBlockedPerDayDto
{
    public required DateTimeOffset Date { get; set; }

    public string DatePersian => Date.ToLocalTime().ToShamsiWithoutTime();

    public required int Count { get; set; }
}