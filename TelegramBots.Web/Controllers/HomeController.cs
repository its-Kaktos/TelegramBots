using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using TelegramBots.Web.Meditr.Home.Statistics;
using TelegramBots.Web.Models;

namespace TelegramBots.Web.Controllers;

[Authorize]
public class HomeController : BaseController
{
    private readonly ILogger _logger;

    public HomeController(ILogger logger)
    {
        _logger = logger.ForContext<HomeController>();
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Statistics()
    {
        var model = await Mediator.Send(new GetStatisticsQuery());

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}