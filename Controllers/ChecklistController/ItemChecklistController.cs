using Microsoft.AspNetCore.Mvc;

namespace ProjetoISP.Baldan.Controllers
{
    /// <summary>
    /// Endereço antigo (/ItemChecklist). As perguntas agora ficam na tela "Perguntas" (/Perguntas).
    /// </summary>
    public class ItemChecklistController : Controller
    {
        public IActionResult Index(string? idModeloChecklist) =>
            RedirectToActionPermanent("Index", "Perguntas", new { idChecklist = idModeloChecklist });
    }
}
