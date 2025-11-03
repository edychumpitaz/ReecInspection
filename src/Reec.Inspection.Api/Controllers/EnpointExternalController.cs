using Microsoft.AspNetCore.Mvc;

namespace Reec.Inspection.Api.Controllers
{
    public class EnpointExternalController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
