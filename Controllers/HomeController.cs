using System.Diagnostics;
using CTOM.Models;
using Microsoft.AspNetCore.Mvc;

namespace CTOM.Controllers
{
    public class HomeController(ILogger<HomeController> logger) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;

        public IActionResult Index()
        {
            _logger.LogInformation("Truy cập trang chủ");
            return View();
        }

        public IActionResult Privacy()
        {
            _logger.LogInformation("Truy cập trang bảo mật");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Đã xảy ra lỗi. Request ID: {RequestId}", requestId);
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
