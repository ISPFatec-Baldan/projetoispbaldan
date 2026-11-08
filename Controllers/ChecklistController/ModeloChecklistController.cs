using Microsoft.AspNetCore.Mvc;

namespace ProjetoISP.Baldan.Controllers.ChecklistController
{
    public class ModeloChecklistController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
