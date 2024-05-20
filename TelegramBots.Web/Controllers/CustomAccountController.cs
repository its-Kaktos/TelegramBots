using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using TelegramBots.Web.ViewModels.Account;

namespace TelegramBots.Web.Controllers;

public class CustomAccountController : Controller
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public CustomAccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ILogger logger,
        IMediator mediator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger.ForContext<CustomAccountController>();
        _mediator = mediator;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User)) return RedirectToAction("Index", "Home");

        ViewData["returnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User)) return RedirectToAction("Index", "Home");

        ViewData["returnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.UserEmail);
            if (user == null)
            {
                _logger
                    .ForContext("GivenEmail", model.UserEmail)
                    .Information("Attempt to login with email that does not exists");

                ModelState.AddModelError(string.Empty, "رمزعبور یا ایمیل وارد شده اشتباه است");
                return View(model);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, model.RememberMe);

                _logger
                    .ForContext("UserId", user.Id)
                    .ForContext("UserName", user.UserName)
                    .Information("User logged in successfully");

                return RedirectToLocalIfReturnUrlIsNotEmptyElseRedirectToHome(returnUrl);
            }

            if (!user.EmailConfirmed && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                _logger
                    .ForContext("UserId", user.Id)
                    .ForContext("UserName", user.UserName)
                    .Information("User login unsuccessful, email is not confirmed");

                ViewData["Message"] = "ایمیل شما تایید نشده و به همین دلیل نمیتوانید وارد حساب کاربری خود بشوید.";
                return View(model);
            }

            if (result.IsLockedOut)
            {
                _logger
                    .ForContext("UserId", user.Id)
                    .ForContext("UserName", user.UserName)
                    .Information("User login unsuccessful, account is locked out");

                ViewData["Message"] = "اکانت شما به دلیل پنج بار ورود ناموفق به مدت 15 دقیقه قفل شده است";
                return View(model);
            }

            _logger
                .ForContext("UserId", user.Id)
                .ForContext("UserName", user.UserName)
                .Information("User login unsuccessful, password is not correct");

            ModelState.AddModelError("", "رمزعبور یا ایمیل وارد شده اشتباه است");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> LogOut()
    {
        if (_signInManager.IsSignedIn(User))
        {
            _logger.Information("User logged out of their account successfully");

            await _signInManager.SignOutAsync();
        }

        return RedirectToAction("Index", "Home");
    }

    #region Helpers

    [NonAction]
    private void AddModelErrorsToModelState(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
    }

    private IActionResult RedirectToLocalIfReturnUrlIsNotEmptyElseRedirectToHome(string returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    #endregion
}