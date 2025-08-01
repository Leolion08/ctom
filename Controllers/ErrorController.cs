using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CTOM.Controllers
{
    [AllowAnonymous]
    [Route("Error")]
    public class ErrorController(ILogger<ErrorController> logger) : Controller
    {
        private readonly ILogger<ErrorController> _logger = logger;

        [Route("AccessDenied")]
        public IActionResult AccessDenied()
        {
            _logger.LogWarning("Truy cập bị từ chối - Người dùng không có quyền");
            return View();
        }
    }
}
