using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using TelegramBots.Web;
using TelegramBots.Web.Common.MediatrBehaviours;
using TelegramBots.Web.Data;
using TelegramBots.Web.Middlewares;
using TelegramBots.Web.Services;

// TODO Only show data to login users
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
        .WithDefaultDestructurers()
        .WithDestructurers(new[] { new DbUpdateExceptionDestructurer() })));

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("mssql")));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options => { options.LoginPath = "/CustomAccount/Login"; });

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddOptions<ConnectionStringsDto>()
    .Configure(cn =>
    {
        cn.Metrics = builder.Configuration.GetConnectionString("Instagram69botMetrics") ?? throw new InvalidOperationException();
        cn.InstagramBot = builder.Configuration.GetConnectionString("Instagram69bot") ?? throw new InvalidOperationException();
    });

builder.Services.AddTransient<SeedDatabaseService>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(CommandsLoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCustomStatusCodeHandler();
app.UseCustomExceptionLoggerHandler();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

var logger = app.Services.GetRequiredService<ILogger>();
logger.Debug("Starting application");
try
{
    using (var scope = app.Services.CreateScope())
    {
        var seedDb = scope.ServiceProvider.GetRequiredService<SeedDatabaseService>();
        await seedDb.SeedDatabaseAsync();
    }

    await app.RunAsync();
}
catch (Exception e)
{
    logger.Fatal(e, "Host terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}