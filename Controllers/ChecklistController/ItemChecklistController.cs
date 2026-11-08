using Microsoft.AspNetCore.Mvc;

namespace ProjetoISP.Baldan.Controllers.ChecklistController
{
    public class ItemChecklistController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
