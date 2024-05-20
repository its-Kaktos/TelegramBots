using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TelegramBots.Web.Data;

namespace TelegramBots.Web.Services;

public class SeedDatabaseService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly UserManager<IdentityUser> _userManager;

    public SeedDatabaseService(ApplicationDbContext dbContext, ILogger logger, UserManager<IdentityUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger.ForContext<SeedDatabaseService>() ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
        _logger.Debug("Database has been migrated");

        var isAnyUserInDb = await _userManager.Users.AnyAsync(cancellationToken: cancellationToken);
        if (!isAnyUserInDb)
        {
            _logger.Debug("Starting seeding database");

            var kaktosUser = new IdentityUser()
            {
                UserName = "kaktos",
                Email = "arman.hmpr@gmail.com",
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(kaktosUser, "98@#5233LKJcn%6882");
            if (!result.Succeeded)
            {
                _logger.Warning("Creating user {Username} failed because {@ErrorMessages}",
                    kaktosUser.UserName, result.Errors);
            }

            var mohsenUser = new IdentityUser()
            {
                UserName = "mohsen691",
                Email = "mohsen691@gmail.com",
                EmailConfirmed = true
            };

            result = await _userManager.CreateAsync(mohsenUser, "asAvIJK23214*(0j@&jdls");
            if (!result.Succeeded)
            {
                _logger.Warning("Creating user {Username} failed because {@ErrorMessages}",
                    mohsenUser.UserName, result.Errors);
            }

            _logger.Debug("Seeding database finished successfully");
        }
    }
}