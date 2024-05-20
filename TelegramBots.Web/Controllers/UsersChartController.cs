using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using TelegramBots.Web.Meditr.UsersChart.GetUserBlockedPerDay;
using TelegramBots.Web.Meditr.UsersChart.GetUserJoinedBotPerDay;
using TelegramBots.Web.Meditr.UsersChart.GetUserJoinedChannelsByBotPerDay;
using TelegramBots.Web.Meditr.UsersChart.GetUserUnBlockedPerDay;
using TelegramBots.Web.Meditr.UsersChart.GetUserUsagesPerDay;

namespace TelegramBots.Web.Controllers;

[Authorize]
public class UsersChartController : BaseController
{
    private readonly ILogger _logger;

    public UsersChartController(ILogger logger)
    {
        _logger = logger.ForContext<UsersChartController>();
    }

    public async Task<IActionResult> GetUserJoinedBotPerDay()
    {
        var result = await Mediator.Send(new GetUserJoinedBotPerDayQuery());

        return Json(result);
    }

    public async Task<IActionResult> GetUserBlockedBotPerDay()
    {
        var result = await Mediator.Send(new GetUserBlockedPerDayQuery());

        return Json(result);
    }

    public async Task<IActionResult> GetUserUnBlockedBotPerDay()
    {
        var result = await Mediator.Send(new GetUserUnBlockedPerDayQuery());

        return Json(result);
    }

    public async Task<IActionResult> GetUserJoinedChannelsByBotPerDay()
    {
        var result = await Mediator.Send(new GetUserJoinedChannelsByBotPerDayQuery());

        return Json(result);
    }

    public async Task<IActionResult> GetUserUsagesPerDay()
    {
        var result = await Mediator.Send(new GetUserUsagesPerDayQuery());

        return Json(result);
    }
}