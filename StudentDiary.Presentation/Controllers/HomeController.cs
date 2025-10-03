using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudentDiary.Presentation.Models;

namespace StudentDiary.Presentation.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // If user is logged in, redirect to diary
        if (HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Index", "Diary");
        }
        
        // Otherwise, redirect to login
        return RedirectToAction("Login", "Auth");
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
