using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Dashboard.Areas.Elsa.Controllers
{
    [Authorize]
    [Area("Elsa")]
    [Route("[area]/[controller]")]
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}