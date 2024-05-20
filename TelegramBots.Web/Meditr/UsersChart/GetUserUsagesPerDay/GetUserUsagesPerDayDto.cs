using System;
using TelegramBots.Web.Common.ExtensionMethods;

namespace TelegramBots.Web.Meditr.UsersChart.GetUserUsagesPerDay;

public class GetUserUsagesPerDayDto
{
    public required DateTimeOffset Date { get; set; }

    public string DatePersian => Date.ToLocalTime().ToShamsiWithoutTime();

    public required int RequestsCount { get; set; }
    public required int SuccessfulCount { get; set; }
    public required int ErrorCount { get; set; }
}