using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjetoISP.Baldan.Models;

namespace ProjetoISP.Baldan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Tela inicial (dashboard).
        // TODO: trocar DashboardViewModel.Exemplo() pelas consultas reais ao Firestore.
        public IActionResult Index()
        {
            return View(DashboardViewModel.Exemplo());
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
