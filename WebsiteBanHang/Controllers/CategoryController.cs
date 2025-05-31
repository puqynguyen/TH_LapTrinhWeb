using Microsoft.AspNetCore.Mvc;

namespace WebsiteBanHang.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
