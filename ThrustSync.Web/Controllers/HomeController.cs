using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ThrustSync.Web.Controllers;

/// <summary>
/// Home controller for the application
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Home/index page
    /// </summary>
    public IActionResult Index()
    {
        _logger.LogInformation("Home Index page accessed");
        return View();
    }

    /// <summary>
    /// Privacy page
    /// </summary>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Error page
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
