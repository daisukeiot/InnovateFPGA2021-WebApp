using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using InnovateFPGA2021_WebApp.Helper;
using InnovateFPGA2021_WebApp.Models;

namespace Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppSettings _appSettings;
        private HomeView _homeView;

        public HomeController(IOptions<AppSettings> optionsAccessor, ILogger<HomeController> logger)
        {
            _logger = logger;
            _appSettings = optionsAccessor.Value;
            _logger.LogInformation("HomeController");
            _homeView = new HomeView();
        }

        public IActionResult Index()
        {
            HomeView homeView = _homeView;
            return View(homeView);
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
}
